namespace KupeServer.Api.Features.Auth.Dtos;

public class TokenResponseDto
{
    public required string AccessToken { get; set; } = string.Empty;
    public required string RefreshToken { get; set; } = string.Empty;
}
