namespace KupeServer.Api.Features.Countries.Dtos;

public record CountryResponseDto(Guid Id, string Iso2, string Name);

public record LanguageResponseDto(Guid Id, string Code, string? Name);
