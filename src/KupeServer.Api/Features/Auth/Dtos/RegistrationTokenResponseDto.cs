namespace KupeServer.Api.Features.Auth.Dtos;

public class RegistrationTokenResponseDto
{
    public required string RegistrationToken { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }

    public required string Email { get; set; }

    public string? Provider { get; set; }
}
