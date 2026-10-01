namespace KupeServer.Api.Features.Countries.Entities;

public class Place
{
    public Guid Id { get; set; }
    public int GeonameId { get; set; }
    public string Name { get; set; } = null!;
    public string AsciiName { get; set; } = null!;
    public Guid CountryId { get; set; }
    public string? Admin1Code { get; set; }
    public string FeatureCode { get; set; } = null!;
    public long Population { get; set; }
    public int? Elevation { get; set; }
    public string? Timezone { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public string? WikidataId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Country Country { get; set; } = null!;
    public ICollection<PlaceName> Names { get; set; } = new List<PlaceName>();
}