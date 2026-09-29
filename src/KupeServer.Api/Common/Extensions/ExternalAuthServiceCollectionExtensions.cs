using KupeServer.Api.Features.Auth;
using KupeServer.Api.Features.Auth.ExternalProviders;

namespace KupeServer.Api.Common.Extensions;

public static class ExternalAuthServiceCollectionExtensions
{
    public static IServiceCollection AddExternalAuthProviders(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ExternalAuthProviderResolver>();
        services.AddScoped<ExternalAuthService>();

        var google = configuration.GetSection(GoogleAuthOptions.SectionName);
        var clientIds = google.Get<GoogleAuthOptions>()?.ClientIds;

        Console.WriteLine($"[Google auth] section='{GoogleAuthOptions.SectionName}' exists={google.Exists()} clientIds={clientIds?.Length ?? -1}");
        Console.WriteLine($"[Google auth] keys under 'Authentication': [{string.Join(", ", configuration.GetSection("Authentication").GetChildren().Select(c => c.Key))}]");

        if (clientIds is { Length: > 0 })
        {
            services.Configure<GoogleAuthOptions>(google);
            services.AddSingleton<IExternalAuthProvider, GoogleAuthProvider>();
        }

        return services;
    }
}