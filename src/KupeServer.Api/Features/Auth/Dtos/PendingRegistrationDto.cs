using System.Text.Json.Serialization;

namespace KupeServer.Api.Features.Auth;

public record PendingExternalIdentityDto(string Provider, string ProviderUserId, string? Email);

public record PendingRegistrationDto(
    string Email,
    string? PasswordHash,
    PendingExternalIdentityDto? ExternalIdentity)
{
    [JsonIgnore]
    public bool IsExternal => ExternalIdentity is not null;
}
