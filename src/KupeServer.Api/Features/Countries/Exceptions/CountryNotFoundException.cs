using KupeServer.Api.Common.Exceptions;

namespace KupeServer.Api.Features.Countries.Exceptions;

public class CountryNotFoundException : DomainException
{
    public CountryNotFoundException(Guid countryId)
        : base($"Country '{countryId}' does not exist.", "COUNTRY_NOT_FOUND") { }
}
