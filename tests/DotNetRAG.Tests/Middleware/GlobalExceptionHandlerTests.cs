using System.Text.Json;
using DotNetRAG.Api.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetRAG.Tests.Middleware;

public class GlobalExceptionHandlerTests
{
    private readonly GlobalExceptionHandler _handler = new(
        NullLogger<GlobalExceptionHandler>.Instance);

    [Fact]
    public async Task HttpRequestException_Returns502WithGenericMessage()
    {
        var (statusCode, detail) = await InvokeHandler(new HttpRequestException("secret upstream error body"));

        statusCode.Should().Be(502);
        detail.Should().Be("An upstream service error occurred. Please try again later.");
    }

    [Fact]
    public async Task DirectoryNotFoundException_Returns404WithGenericMessage()
    {
        var (statusCode, detail) = await InvokeHandler(new DirectoryNotFoundException("/secret/server/path"));

        statusCode.Should().Be(404);
        detail.Should().Be("The specified directory was not found.");
        detail.Should().NotContain("/secret/server/path");
    }

    [Fact]
    public async Task OperationCanceledException_Returns499()
    {
        var (statusCode, detail) = await InvokeHandler(new OperationCanceledException());

        statusCode.Should().Be(499);
        detail.Should().Be("Request was cancelled.");
    }

    [Fact]
    public async Task UnknownException_Returns500WithGenericMessage()
    {
        var (statusCode, detail) = await InvokeHandler(new InvalidOperationException("internal details"));

        statusCode.Should().Be(500);
        detail.Should().Be("An unexpected error occurred.");
        detail.Should().NotContain("internal details");
    }

    private async Task<(int StatusCode, string? Detail)> InvokeHandler(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        result.Should().BeTrue();

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            context.Response.Body);

        return (context.Response.StatusCode, problemDetails?.Detail);
    }
}
