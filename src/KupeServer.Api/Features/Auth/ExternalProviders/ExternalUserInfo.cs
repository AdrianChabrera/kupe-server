namespace KupeServer.Api.Features.Auth.ExternalProviders;

public record ExternalUserInfo(
    string Provider,
    string ProviderUserId,
    string? Email,
    bool EmailVerified,
    string? Name);