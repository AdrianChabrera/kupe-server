using System.ComponentModel.DataAnnotations;

namespace KupeServer.Api.Features.Auth.Dtos;

public class CompleteRegistrationDto
{
    [Required(ErrorMessage = "Registration token is required.")]
    public string RegistrationToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "Username is required.")]
    [StringLength(32, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 32 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9_.-]+$", ErrorMessage = "Username can only contain letters, numbers, '.', '_' and '-'.")]
    public string Username { get; set; } = string.Empty;

    public Guid? CountryId { get; set; }

    [MaxLength(20, ErrorMessage = "You can select up to 20 languages.")]
    public List<Guid>? LanguageIds { get; set; }
}
