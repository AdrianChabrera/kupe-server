using KupeServer.Api.Common.Exceptions;

namespace KupeServer.Api.Features.Auth.Exceptions;

public class UnsupportedExternalProviderException : DomainException
{
    public UnsupportedExternalProviderException(string provider)
        : base($"External provider '{provider}' is not supported.", "UNSUPPORTED_EXTERNAL_PROVIDER") { }
}