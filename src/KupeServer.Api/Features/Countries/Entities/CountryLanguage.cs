namespace KupeServer.Api.Features.Countries.Entities;

public class CountryLanguage
{
    public Guid CountryId { get; set; }
    public Guid LanguageId { get; set; }
    public int Position { get; set; }
    
    public Country Country { get; set; } = null!;
    public Language Language { get; set; } = null!;
}