using Google.Apis.Auth;
using KupeServer.Api.Features.Auth.Exceptions;
using Microsoft.Extensions.Options;

namespace KupeServer.Api.Features.Auth.ExternalProviders;

public class GoogleAuthProvider(IOptions<GoogleAuthOptions> options) : IExternalAuthProvider
{
    public string Name => "google";

    public async Task<ExternalUserInfo> AuthenticateAsync(string credential, CancellationToken cancellationToken = default)
    {
        try 
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = options.Value.ClientIds
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(credential, settings);

            return new ExternalUserInfo(
                Provider: Name,
                ProviderUserId: payload.Subject,
                Email: payload.Email,
                EmailVerified: payload.EmailVerified,
                Name: payload.Name);
        }
        catch (InvalidJwtException)
        {
            throw new InvalidExternalTokenException(Name);
        }
    }
}   