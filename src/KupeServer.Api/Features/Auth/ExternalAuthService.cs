using System.Security.Cryptography;
using System.Text.RegularExpressions;
using KupeServer.Api.Data;
using KupeServer.Api.Features.Auth.Dtos;
using KupeServer.Api.Features.Auth.Entities;
using KupeServer.Api.Features.Auth.Exceptions;
using KupeServer.Api.Features.Auth.ExternalProviders;
using KupeServer.Api.Features.Users.Entities;
using KupeServer.Api.Features.Users.Enums;
using Microsoft.EntityFrameworkCore;

namespace KupeServer.Api.Features.Auth;

public partial class ExternalAuthService(
    AppDbContext context,
    AuthService authService,
    ExternalAuthProviderResolver providerResolver)
{
    public async Task<TokenResponseDto> Login(string providerName, string credential, CancellationToken ct = default)
    {
        var info = await providerResolver.Resolve(providerName).AuthenticateAsync(credential, ct);

        var identity = await context.ExternalIdentities
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Provider == info.Provider && i.ProviderUserId == info.ProviderUserId, ct);

        var user = identity?.User ?? await CreateUserFromExternalProfile(info, ct);

        return await authService.CreateTokenResponse(user);
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

        context.ExternalIdentities.Add(NewIdentity(userId, info));
        await context.SaveChangesAsync(ct);
    }

    private async Task<User> CreateUserFromExternalProfile(ExternalUserInfo info, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(info.Email) || !info.EmailVerified)
        {
            throw new ExternalEmailNotVerifiedException(info.Provider);
        }

        if (await context.Users.AnyAsync(u => u.Email == info.Email, ct))
        {
            throw new ExternalEmailConflictException();
        }

        var user = new User
        {
            Username = await GenerateUniqueUsername(info, ct),
            Email = info.Email,
            PasswordHash = null,
            Role = UserRole.User
        };

        user.ExternalIdentities.Add(NewIdentity(user.Id, info));

        context.Users.Add(user);
        await context.SaveChangesAsync(ct);

        return user;
    }

    private static ExternalIdentity NewIdentity(Guid userId, ExternalUserInfo info) => new()
    {
        UserId = userId,
        Provider = info.Provider,
        ProviderUserId = info.ProviderUserId,
        Email = info.Email
    };

    private async Task<string> GenerateUniqueUsername(ExternalUserInfo info, CancellationToken ct)
    {
        var source = info.Email!.Split('@')[0];
        var baseName = InvalidUsernameChars().Replace(source, string.Empty);

        if (baseName.Length < 3)
        {
            baseName = "user";
        }

        baseName = baseName[..Math.Min(baseName.Length, 24)];

        var candidate = baseName;
        while (await context.Users.AnyAsync(u => u.Username == candidate, ct))
        {
            candidate = $"{baseName}{RandomNumberGenerator.GetInt32(1000, 10000)}";
        }

        return candidate;
    }

    [GeneratedRegex("[^a-zA-Z0-9_.-]")]
    private static partial Regex InvalidUsernameChars();
}