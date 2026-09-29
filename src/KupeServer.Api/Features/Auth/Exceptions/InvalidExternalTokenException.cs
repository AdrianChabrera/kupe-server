using KupeServer.Api.Common.Exceptions;

namespace KupeServer.Api.Features.Auth.Exceptions;

public class InvalidExternalTokenException : DomainException
{
    public InvalidExternalTokenException(string provider)
        : base($"Invalid or expired {provider} token.", "INVALID_EXTERNAL_TOKEN") { }
}