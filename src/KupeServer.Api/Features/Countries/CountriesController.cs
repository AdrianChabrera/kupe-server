using KupeServer.Api.Features.Countries.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace KupeServer.Api.Features.Countries;

[Route("api/countries")]
[ApiController]
public class CountriesController(CountriesService countriesService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CountryResponseDto>>> GetAllCountries(CancellationToken ct)
    {
        var countries = await countriesService.findAllCountries(ct);

        return Ok(countries);
    }

    [HttpGet]
    public async Task<ActionResult<List<LanguageResponseDto>>> GetAllLanguages(CancellationToken ct)
    {
        var languages = await countriesService.findAllLanguages(ct);

        return Ok(languages);
    }

    [HttpGet("{countryId:guid}/languages")]
    public async Task<ActionResult<List<LanguageResponseDto>>> GetCountryLanguages(Guid countryId, CancellationToken ct)
    {
        var languages = await countriesService.findCountryLanguages(countryId, ct);

        return Ok(languages);
    }
}
