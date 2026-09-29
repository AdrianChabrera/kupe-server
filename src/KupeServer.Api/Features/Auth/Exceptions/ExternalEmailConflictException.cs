using KupeServer.Api.Common.Exceptions;

namespace KupeServer.Api.Features.Auth.Exceptions;

public class ExternalEmailConflictException : DomainException
{
    public ExternalEmailConflictException()
        : base("An account with this email already exists. Log in with your credentials and link the external account from your profile.",
            "EXTERNAL_EMAIL_CONFLICT") { }
}