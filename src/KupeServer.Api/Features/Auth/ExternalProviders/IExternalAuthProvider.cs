namespace KupeServer.Api.Features.Auth.ExternalProviders;

public interface IExternalAuthProvider
{
    string Name { get; }

    Task<ExternalUserInfo> AuthenticateAsync(string credential, CancellationToken cancellationToken = default);
}