using System.ComponentModel.DataAnnotations;

namespace KupeServer.Api.Features.Auth.Dtos;

public class ExternalLoginRequestDto
{
    [Required(ErrorMessage = "Token is required.")]
    public string Token { get; set; } = string.Empty;
}