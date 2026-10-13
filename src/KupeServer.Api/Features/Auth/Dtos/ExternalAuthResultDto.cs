using System.Text.Json.Serialization;

namespace KupeServer.Api.Features.Auth.Dtos;

public enum ExternalAuthStatus
{
    Authenticated,
    RegistrationRequired
}

public class ExternalAuthResultDto
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ExternalAuthStatus Status { get; init; }

    public TokenResponseDto? Tokens { get; init; }
    public RegistrationTokenResponseDto? Registration { get; init; }

    public static ExternalAuthResultDto Authenticated(TokenResponseDto tokens) =>
        new() { Status = ExternalAuthStatus.Authenticated, Tokens = tokens };

    public static ExternalAuthResultDto RegistrationRequired(RegistrationTokenResponseDto registration) =>
        new() { Status = ExternalAuthStatus.RegistrationRequired, Registration = registration };
}
