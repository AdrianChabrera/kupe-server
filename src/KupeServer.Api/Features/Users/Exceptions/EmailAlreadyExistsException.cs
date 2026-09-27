using KupeServer.Api.Common.Exceptions;

namespace KupeServer.Api.Features.Users.Exceptions;

public class EmailAlreadyExistsException : DomainException
{
    public EmailAlreadyExistsException(string email)
        : base($"Email '{email}' already exists.", "EMAIL_ALREADY_EXISTS")
    {
    }
}