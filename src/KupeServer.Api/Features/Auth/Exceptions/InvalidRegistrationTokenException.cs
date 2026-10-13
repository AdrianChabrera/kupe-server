using KupeServer.Api.Common.Exceptions;

namespace KupeServer.Api.Features.Auth.Exceptions;

public class InvalidRegistrationTokenException : DomainException
{
    public InvalidRegistrationTokenException()
        : base("The registration token is invalid or has expired. Please start the registration again.",
            "INVALID_REGISTRATION_TOKEN") { }
}
