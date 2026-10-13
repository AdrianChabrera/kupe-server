using KupeServer.Api.Common.Exceptions;

namespace KupeServer.Api.Features.Countries.Exceptions;

public class LanguageNotFoundException : DomainException
{
    public LanguageNotFoundException(IEnumerable<Guid> languageIds)
        : base($"Unknown language id(s): {string.Join(", ", languageIds)}.", "LANGUAGE_NOT_FOUND") { }
}
