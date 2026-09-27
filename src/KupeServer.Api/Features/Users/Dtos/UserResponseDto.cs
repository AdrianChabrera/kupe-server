using KupeServer.Api.Features.Users.Entities;
using KupeServer.Api.Features.Users.Enums;

namespace KupeServer.Api.Features.Users.Dtos;

public class UserResponseDto(User user)
{
    public Guid Id { get; set; } = user.Id;
    public UserRole Role { get; set; } = user.Role;
    public string Username { get; set; } = user.Username;
    public string Email { get; set; } = user.Email;
}
