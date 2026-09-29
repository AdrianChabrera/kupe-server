namespace KupeServer.Api.Features.Auth.ExternalProviders;

public class GoogleAuthOptions
{
    public const string SectionName = "ExternalAuth:Google";

    public string[] ClientIds { get; set; } = [];
}