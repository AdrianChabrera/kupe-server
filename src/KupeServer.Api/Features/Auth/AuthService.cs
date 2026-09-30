using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using KupeServer.Api.Data;
using KupeServer.Api.Features.Auth.Dtos;
using KupeServer.Api.Features.Auth.Exceptions;
using KupeServer.Api.Features.Users.Dtos;
using KupeServer.Api.Features.Users.Entities;
using KupeServer.Api.Features.Users.Enums;
using KupeServer.Api.Features.Users.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace KupeServer.Api.Features.Auth;

public class AuthService(AppDbContext context, IConfiguration configuration, IPasswordHasher<User> passwordHasher)
{
    public async Task<User> Register(RegisterDto request)
    {
        if (await context.Users.AnyAsync(u => u.Username == request.Username))
        {
            throw new UsernameAlreadyExistsException(request.Username);
        }

        var email = request.Email.Trim().ToLowerInvariant();

        if (await context.Users.AnyAsync(u => u.Email == email))
        {
            throw new EmailAlreadyExistsException(email);
        }

        var user = new User();
        var hashedPassword = passwordHasher.HashPassword(user, request.Password);

            
        user.Username = request.Username;
        user.Email = email;
        user.PasswordHash = hashedPassword;
        user.Role = UserRole.User;

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
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