namespace Erp.Web.Config;

public class OidcSettings
{
    public string Authority { get; set; } = default!;
    public string ClientId { get; set; } = default!;
    public string ResponseType { get; set; } = "code";
    public bool SaveTokens { get; set; } = true;
    public bool RequireHttpsMetadata { get; set; } = true;
    public string RedirectUri { get; set; } = default!;
    public string PostLogoutRedirectUri { get; set; } = default!;
    public List<string> Scopes { get; set; } = new();
}
