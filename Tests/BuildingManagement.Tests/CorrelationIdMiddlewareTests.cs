using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BuildingManagement.Tests;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_GeneratesServerOwnedIdAndReturnsItInResponseHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.ResponseHeaderName] = "client-controlled";
        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var correlationId = context.Response.Headers[CorrelationIdMiddleware.ResponseHeaderName].ToString();
        Assert.Matches("^[a-f0-9]{32}$", correlationId);
        Assert.Equal(correlationId, context.TraceIdentifier);
        Assert.NotEqual("client-controlled", correlationId);
    }

    [Fact]
    public async Task InvokeAsync_UsesUniqueIdentifierForEachRequest()
    {
        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            NullLogger<CorrelationIdMiddleware>.Instance);
        var first = new DefaultHttpContext();
        var second = new DefaultHttpContext();

        await middleware.InvokeAsync(first);
        await middleware.InvokeAsync(second);

        Assert.NotEqual(
            first.Response.Headers[CorrelationIdMiddleware.ResponseHeaderName].ToString(),
            second.Response.Headers[CorrelationIdMiddleware.ResponseHeaderName].ToString());
    }
}
