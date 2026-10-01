namespace KupeServer.Api.Features.Countries.Entities;

public class PlaceName
{
    public Guid PlaceId { get; set; }
    public string Language { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsPreferred { get; set; }

    public Place Place { get; set; } = null!;
}