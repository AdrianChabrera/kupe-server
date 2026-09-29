using KupeServer.Api.Features.Auth.Entities;
using KupeServer.Api.Features.Users.Enums;

namespace KupeServer.Api.Features.Users.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public string? RefreshTokenHash { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public ICollection<ExternalIdentity> ExternalIdentities { get; set; } = [];
}