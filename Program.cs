using System.Threading.RateLimiting;
using AuthAPI;
using AuthAPI.Filters;
using AuthAPI.HealthChecks;
using AuthAPI.Middleware;
using AuthAPI.Options;
using AuthAPI.Repositories;
using AuthAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

const string DefaultConnectionKey = "DefaultConnection";
var connectionString = builder.Configuration.GetConnectionString(DefaultConnectionKey)!;
builder.Services.AddSingleton(new DbSettings(connectionString));

// === Options ===
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection(SecurityOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<TokenCleanupOptions>(builder.Configuration.GetSection(TokenCleanupOptions.SectionName));

// === Infrastructure ===
builder.Services.AddSingleton<IConnectionFactory, SqlConnectionFactory>();

// === Repositories ===
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();

// === Services ===
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddHostedService<TokenCleanupService>();

// === Authentication (RS256 JWT) — uses JwtService as single source of truth ===
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IJwtService>((options, jwtService) =>
    {
        options.TokenValidationParameters = jwtService.GetTokenValidationParameters();
    });

builder.Services.AddAuthorization();

// === Forwarded Headers (proxy support for rate limiting) ===
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// === CORS (restricted methods & headers) ===
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                ?? (builder.Environment.IsDevelopment()
                    ? ["http://localhost:3000"]
                    : throw new InvalidOperationException("Cors:Origins must be configured in production")))
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
            .WithHeaders("Authorization", "Content-Type", HttpHeaderNames.ClientId, HttpHeaderNames.ClientSecret, HttpHeaderNames.CorrelationId)
            .AllowCredentials();
    });
});

// === Rate Limiting (proxy-aware, IP-based) ===
var rateLimitOpts = builder.Configuration.GetSection(AuthRateLimitOptions.SectionName).Get<AuthRateLimitOptions>() ?? new();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("auth-limit", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                          ?? context.Connection.RemoteIpAddress?.ToString()
                          ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitOpts.PermitLimit,
                Window = TimeSpan.FromMinutes(rateLimitOpts.WindowMinutes)
            }));
});

// === Health Checks ===
builder.Services.AddHealthChecks()
    .AddCheck<DbHealthCheck>("db");

// === Controllers + Swagger ===
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AuthAPI",
        Version = "v1",
        Description = "Centralized Auth Server — JWT-based authentication with RS256 signing"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT access token"
    });
    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer"), new List<string>() }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
