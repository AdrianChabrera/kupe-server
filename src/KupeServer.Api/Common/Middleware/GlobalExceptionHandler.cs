using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using KupeServer.Api.Common.Exceptions;
using KupeServer.Api.Features.Auth;
using KupeServer.Api.Features.Countries.Exceptions;
using KupeServer.Api.Features.Users.Exceptions;
using KupeServer.Api.Features.Auth.Exceptions;

namespace KupeServer.Api.Common.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = MapException(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred");
        }
        else
        {
            _logger.LogWarning(exception, "Handled domain exception: {Message}", exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
        };

        if (exception is DomainException domainException)
        {
            problem.Extensions["errorCode"] = domainException.ErrorCode;
        }

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }

    private static (int statusCode, string title) MapException(Exception exception)
    {
        return exception switch
        {
            UsernameAlreadyExistsException => (StatusCodes.Status409Conflict, exception.Message),
            EmailAlreadyExistsException => (StatusCodes.Status409Conflict, exception.Message),
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, exception.Message),
            InvalidRefreshTokenException => (StatusCodes.Status401Unauthorized, exception.Message),
            UnsupportedExternalProviderException => (StatusCodes.Status400BadRequest, exception.Message),
            InvalidExternalTokenException => (StatusCodes.Status401Unauthorized, exception.Message),
            ExternalEmailNotVerifiedException => (StatusCodes.Status401Unauthorized, exception.Message),
            ExternalEmailConflictException => (StatusCodes.Status409Conflict, exception.Message),
            ExternalIdentityAlreadyLinkedException => (StatusCodes.Status409Conflict, exception.Message),
            InvalidRegistrationTokenException => (StatusCodes.Status400BadRequest, exception.Message),
            CountryNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            LanguageNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };
    }
}