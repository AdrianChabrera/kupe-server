using KupeServer.Api.Common.Exceptions;

namespace KupeServer.Api.Features.Auth.Exceptions;

public class ExternalIdentityAlreadyLinkedException : DomainException
{
    public ExternalIdentityAlreadyLinkedException(string provider)
        : base($"This {provider} account (or a {provider} account for this user) is already linked.", "EXTERNAL_IDENTITY_ALREADY_LINKED") { }
}