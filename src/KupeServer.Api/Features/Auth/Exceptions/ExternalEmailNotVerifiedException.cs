using KupeServer.Api.Common.Exceptions;

namespace KupeServer.Api.Features.Auth.Exceptions;

public class ExternalEmailNotVerifiedException : DomainException
{
    public ExternalEmailNotVerifiedException(string provider)
        : base($"The {provider} account does not have a verified email.", "EXTERNAL_EMAIL_NOT_VERIFIED") { }
}