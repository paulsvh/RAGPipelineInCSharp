using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace DotNetRAG.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, detail) = exception switch
        {
            HttpRequestException => (StatusCodes.Status502BadGateway,
                "An upstream service error occurred. Please try again later."),
            DirectoryNotFoundException => (StatusCodes.Status404NotFound,
                "The specified directory was not found."),
            OperationCanceledException => (StatusCodes.Status499ClientClosedRequest,
                "Request was cancelled."),
            _ => (StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.")
        };

        logger.LogError(exception, "Request failed with {StatusCode}: {Detail}", statusCode, detail);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Detail = detail
        }, cancellationToken);

        return true;
    }
}
