namespace AuthAPI.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HttpHeaderNames.CorrelationId].FirstOrDefault()
                            ?? context.TraceIdentifier;

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HttpHeaderNames.CorrelationId] = correlationId;

        using (context.RequestServices.GetRequiredService<ILoggerFactory>()
                   .CreateLogger("CorrelationId")
                   .BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }
}
