namespace KupeServer.Api.Features.Countries.Entities;

public class Language
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string? Name { get; set; }

    public ICollection<CountryLanguage> Countries { get; set; } = new List<CountryLanguage>();
}