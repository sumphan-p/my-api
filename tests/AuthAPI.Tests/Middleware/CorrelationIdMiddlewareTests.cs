using AuthAPI.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuthAPI.Tests.Middleware;

public class CorrelationIdMiddlewareTests
{
    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.RequestServices = new FakeServiceProvider();
        return context;
    }

    [Fact]
    public async Task InvokeAsync_NoHeader_UsesTraceIdentifier()
    {
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next);
        var context = CreateContext();
        var originalTraceId = context.TraceIdentifier;

        await middleware.InvokeAsync(context);

        Assert.Equal(originalTraceId, context.Response.Headers["X-Correlation-Id"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_WithHeader_UsesProvidedId()
    {
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next);
        var context = CreateContext();
        context.Request.Headers["X-Correlation-Id"] = "my-custom-id";

        await middleware.InvokeAsync(context);

        Assert.Equal("my-custom-id", context.TraceIdentifier);
        Assert.Equal("my-custom-id", context.Response.Headers["X-Correlation-Id"].ToString());
    }

    private class FakeServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(ILoggerFactory))
                return NullLoggerFactory.Instance;
            return null;
        }
    }
}
