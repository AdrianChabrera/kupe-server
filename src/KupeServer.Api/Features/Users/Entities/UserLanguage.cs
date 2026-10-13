using KupeServer.Api.Features.Countries.Entities;

namespace KupeServer.Api.Features.Users.Entities;

public class UserLanguage
{
    public Guid UserId { get; set; }
    public Guid LanguageId { get; set; }

    public User User { get; set; } = null!;
    public Language Language { get; set; } = null!;
}
