using KupeServer.Api.Features.Auth.Exceptions;

namespace KupeServer.Api.Features.Auth.ExternalProviders;

public class ExternalAuthProviderResolver(IEnumerable<IExternalAuthProvider> providers)
{
    private readonly Dictionary<string, IExternalAuthProvider> _providers =
        providers.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

    public IExternalAuthProvider Resolve(string name) =>
        _providers.TryGetValue(name, out var provider)
            ? provider
            : throw new UnsupportedExternalProviderException(name);
}