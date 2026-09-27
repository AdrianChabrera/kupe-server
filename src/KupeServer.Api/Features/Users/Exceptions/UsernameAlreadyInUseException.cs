using KupeServer.Api.Common.Exceptions;

namespace KupeServer.Api.Features.Users.Exceptions;

public class UsernameAlreadyExistsException : DomainException
{
    public UsernameAlreadyExistsException(string username)
        : base($"Username '{username}' already exists.", "USERNAME_ALREADY_EXISTS")
    {
    }
}