using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using KupeServer.Api.Data;
using KupeServer.Api.Features.Auth.Dtos;
using KupeServer.Api.Features.Auth.Entities;
using KupeServer.Api.Features.Auth.Exceptions;
using KupeServer.Api.Features.Countries.Exceptions;
using KupeServer.Api.Features.Users.Dtos;
using KupeServer.Api.Features.Users.Entities;
using KupeServer.Api.Features.Users.Enums;
using KupeServer.Api.Features.Users.Exceptions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace KupeServer.Api.Features.Auth;

public class AuthService(
    AppDbContext context,
    IConfiguration configuration,
    IPasswordHasher<User> passwordHasher,
    IDataProtectionProvider dataProtectionProvider)
{
    private static readonly TimeSpan RegistrationTokenLifetime = TimeSpan.FromMinutes(30);

    private readonly ITimeLimitedDataProtector _registrationProtector = dataProtectionProvider
        .CreateProtector("KupeServer.Auth.PendingRegistration.v1")
        .ToTimeLimitedDataProtector();

    public async Task<RegistrationTokenResponseDto> StartRegistration(
        RegisterDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await context.Users.AnyAsync(u => u.Email == email, ct))
        {
            throw new EmailAlreadyExistsException(email);
        }

        var pending = new PendingRegistrationDto(
            Email: email,
            PasswordHash: passwordHasher.HashPassword(new User(), request.Password),
            ExternalIdentity: null);

        return CreateRegistrationResponse(pending);
    }

    public RegistrationTokenResponseDto CreateRegistrationResponse(PendingRegistrationDto pending)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(RegistrationTokenLifetime);
        var token = _registrationProtector.Protect(JsonSerializer.Serialize(pending), expiresAt);

        return new RegistrationTokenResponseDto
        {
            RegistrationToken = token,
            ExpiresAt = expiresAt,
            Email = pending.Email,
            Provider = pending.ExternalIdentity?.Provider
        };
    }

    public async Task<TokenResponseDto> CompleteRegistration(
        CompleteRegistrationDto request, CancellationToken ct = default)
    {
        var pending = ReadRegistrationToken(request.RegistrationToken);
        var languageIds = request.LanguageIds?.Distinct().ToList() ?? [];

        await EnsureProfileDataIsValid(request.CountryId, languageIds, ct);

        if (await context.Users.AnyAsync(u => u.Email == pending.Email, ct))
        {
            throw EmailConflict(pending);
        }

        if (await context.Users.AnyAsync(u => u.Username == request.Username, ct))
        {
            throw new UsernameAlreadyExistsException(request.Username);
        }

        if (pending.ExternalIdentity is { } external
            && await context.ExternalIdentities.AnyAsync(i =>
                i.Provider == external.Provider && i.ProviderUserId == external.ProviderUserId, ct))
        {
            throw new ExternalIdentityAlreadyLinkedException(external.Provider);
        }

        var user = new User
        {
            Username = request.Username,
            Email = pending.Email,
            PasswordHash = pending.PasswordHash,
            Role = UserRole.User,
            CountryId = request.CountryId
        };

        foreach (var languageId in languageIds)
        {
            user.Languages.Add(new UserLanguage { LanguageId = languageId });
        }

        if (pending.ExternalIdentity is { } identity)
        {
            user.ExternalIdentities.Add(new ExternalIdentity
            {
                UserId = user.Id,
                Provider = identity.Provider,
                ProviderUserId = identity.ProviderUserId,
                Email = identity.Email
            });
        }

        context.Users.Add(user);

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                                           {
                                               SqlState: PostgresErrorCodes.UniqueViolation
                                           } pg)
        {
            throw MapUniqueViolation(pg, pending, request.Username);
        }

        return await CreateTokenResponse(user);
    }

    private PendingRegistrationDto ReadRegistrationToken(string token)
    {
        try
        {
            var json = _registrationProtector.Unprotect(token);

            return JsonSerializer.Deserialize<PendingRegistrationDto>(json)
                   ?? throw new InvalidRegistrationTokenException();
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException)
        {
            throw new InvalidRegistrationTokenException();
        }
    }

    private async Task EnsureProfileDataIsValid(Guid? countryId, List<Guid> languageIds, CancellationToken ct)
    {
        if (countryId is { } id && !(await context.Countries.AnyAsync(c => c.Id == id, ct)))
        {
            throw new CountryNotFoundException(id);
        }

        if (languageIds.Count == 0)
        {
            return;
        }

        var existing = await context.Languages
            .Where(l => languageIds.Contains(l.Id))
            .Select(l => l.Id)
            .ToListAsync(ct);

        var missing = languageIds.Except(existing).ToList();

        if (missing.Count > 0)
        {
            throw new LanguageNotFoundException(missing);
        }
    }

    private static Exception EmailConflict(PendingRegistrationDto pending) =>
        pending.IsExternal
            ? new ExternalEmailConflictException()
            : new EmailAlreadyExistsException(pending.Email);

    private static Exception MapUniqueViolation(PostgresException pg, PendingRegistrationDto pending, string username)
    {
        var constraint = pg.ConstraintName ?? string.Empty;

        if (constraint.Contains("username", StringComparison.OrdinalIgnoreCase))
        {
            return new UsernameAlreadyExistsException(username);
        }

        if (constraint.Contains("email", StringComparison.OrdinalIgnoreCase))
        {
            return EmailConflict(pending);
        }

        return new ExternalIdentityAlreadyLinkedException(pending.ExternalIdentity?.Provider ?? "external");
    }

    public async Task<TokenResponseDto> Login(LoginDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email) ?? throw new InvalidCredentialsException();
        
        if (user.PasswordHash is null)
        {
            throw new InvalidCredentialsException();
        }
        
        var passwordVerificationResult = passwordHasher.VerifyHashedPassword(
            user, user.PasswordHash, request.Password);

        if (passwordVerificationResult == PasswordVerificationResult.Failed)
        {
            throw new InvalidCredentialsException();
        }

        TokenResponseDto response = await CreateTokenResponse(user);

        return response;
    }

    public async Task<TokenResponseDto> CreateTokenResponse(User user)
    {
        var response = new TokenResponseDto
        {
            AccessToken = CreateToken(user),
            RefreshToken = await GenerateAndSaveRefreshToken(user)
        };

        return response;
    }

    public async Task<TokenResponseDto> RefreshToken(RefreshTokenRequestDto request)
    {
        var user = await ValidateRefreshToken(request.UserId, request.RefreshToken);
        return await CreateTokenResponse(user);
    }

    private async Task<User> ValidateRefreshToken(Guid userId, string refreshToken)
    {
        var user = await context.Users.FindAsync(userId);

        if (user is null
            || user.RefreshTokenHash is null
            || user.RefreshTokenExpiryTime <= DateTime.UtcNow
            || !VerifyRefreshToken(refreshToken, user.RefreshTokenHash))
        {
            throw new InvalidRefreshTokenException();
        }

        return user;
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private async Task<string> GenerateAndSaveRefreshToken(User user)
    {
        var refreshToken = GenerateRefreshToken();

        user.RefreshTokenHash = HashRefreshToken(refreshToken);
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        context.Users.Update(user);
        await context.SaveChangesAsync();

        return refreshToken;
    }

    private static string HashRefreshToken(string refreshToken)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(refreshToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    private static bool VerifyRefreshToken(string refreshToken, string storedHash)
    {
        var incomingHash = HashRefreshToken(refreshToken);

        var incomingBytes = Convert.FromBase64String(incomingHash);
        var storedBytes = Convert.FromBase64String(storedHash);

        return CryptographicOperations.FixedTimeEquals(incomingBytes, storedBytes);
    }

    private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
                configuration["AppSettings:Token"]!
            ));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new JwtSecurityToken(
                issuer: configuration["AppSettings:Issuer"]!,
                audience: configuration["AppSettings:Audience"]!,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }
}