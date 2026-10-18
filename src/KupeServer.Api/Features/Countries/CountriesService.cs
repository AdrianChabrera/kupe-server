using KupeServer.Api.Data;
using KupeServer.Api.Features.Countries.Dtos;
using Microsoft.EntityFrameworkCore;

public class CountriesService(AppDbContext context)
{
    public async Task<List<CountryResponseDto>> findAllCountries(CancellationToken ct)
    {
        var countries = await context.Countries
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CountryResponseDto(c.Id, c.Iso2, c.Name))
            .ToListAsync(ct);
        return countries;
    }

    public async Task<List<LanguageResponseDto>> findAllLanguages(CancellationToken ct)
    {
        var languages = await context.Languages
            .AsNoTracking()
            .OrderBy(l => l.Name ?? l.Code)
            .Select(l => new LanguageResponseDto(l.Id, l.Code, l.Name))
            .ToListAsync(ct);
        return languages;
    }

    public async Task<List<LanguageResponseDto>> findCountryLanguages(Guid countryId, CancellationToken ct)
    {
        var languages = await context.CountryLanguages
            .AsNoTracking()
            .Where(cl => cl.CountryId == countryId)
            .OrderBy(cl => cl.Position)
            .Select(cl => new LanguageResponseDto(cl.Language.Id, cl.Language.Code, cl.Language.Name))
            .ToListAsync(ct);
        return languages;
    }


}