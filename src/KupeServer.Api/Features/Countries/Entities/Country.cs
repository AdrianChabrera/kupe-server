namespace KupeServer.Api.Features.Countries.Entities;

public class Country
{
    public Guid Id { get; set; }
    public string Iso2 { get; set; } = null!;
    public string Iso3 { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Capital { get; set; }
    public string? Continent { get; set; }
    public double? AreaKm2 { get; set; }
    public long? Population { get; set; }
    public string? CurrencyCode { get; set; }
    public string? CurrencyName { get; set; }
    public string? PhonePrefix { get; set; }
    public string? Languages { get; set; }
    public int GeonameId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Place> Places { get; set; } = new List<Place>();
}