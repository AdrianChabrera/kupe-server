using KupeServer.Api.Common.Exceptions;

namespace KupeServer.Api.Features.Auth.Exceptions;

public class InvalidRefreshTokenException : DomainException
{
    public InvalidRefreshTokenException()
        : base("Invalid or expired refresh token.", "INVALID_REFRESH_TOKEN") { }
}