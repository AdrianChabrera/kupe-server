using KupeServer.Api.Data;
using KupeServer.Api.Features.Auth.Dtos;
using KupeServer.Api.Features.Auth.Entities;
using KupeServer.Api.Features.Auth.Exceptions;
using KupeServer.Api.Features.Auth.ExternalProviders;
using Microsoft.EntityFrameworkCore;

namespace KupeServer.Api.Features.Auth;

public class ExternalAuthService(
    AppDbContext context,
    AuthService authService,
    ExternalAuthProviderResolver providerResolver)
{
    public async Task<ExternalAuthResultDto> Login(string providerName, string credential, CancellationToken ct = default)
    {
        var info = await providerResolver.Resolve(providerName).AuthenticateAsync(credential, ct);

        var identity = await context.ExternalIdentities
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Provider == info.Provider && i.ProviderUserId == info.ProviderUserId, ct);

        if (identity is not null)
        {
            return ExternalAuthResultDto.Authenticated(await authService.CreateTokenResponse(identity.User));
        }

        return ExternalAuthResultDto.RegistrationRequired(await StartRegistration(info, ct));
    }

    public async Task Link(Guid userId, string providerName, string credential, CancellationToken ct = default)
    {
        var info = await providerResolver.Resolve(providerName).AuthenticateAsync(credential, ct);

        var alreadyLinked = await context.ExternalIdentities.AnyAsync(i =>
            i.Provider == info.Provider &&
            (i.ProviderUserId == info.ProviderUserId || i.UserId == userId), ct);

        if (alreadyLinked)
        {
            throw new ExternalIdentityAlreadyLinkedException(info.Provider);
        }

        context.ExternalIdentities.Add(new ExternalIdentity
        {
            UserId = userId,
            Provider = info.Provider,
            ProviderUserId = info.ProviderUserId,
            Email = info.Email
        });
        await context.SaveChangesAsync(ct);
    }

    private async Task<RegistrationTokenResponseDto> StartRegistration(ExternalUserInfo info, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(info.Email) || !info.EmailVerified)
        {
            throw new ExternalEmailNotVerifiedException(info.Provider);
        }

        var email = info.Email.Trim().ToLowerInvariant();

        if (await context.Users.AnyAsync(u => u.Email == email, ct))
        {
            throw new ExternalEmailConflictException();
        }

        var pending = new PendingRegistrationDto(
            Email: email,
            PasswordHash: null,
            ExternalIdentity: new PendingExternalIdentityDto(info.Provider, info.ProviderUserId, info.Email));

        return authService.CreateRegistrationResponse(pending);
    }
}
