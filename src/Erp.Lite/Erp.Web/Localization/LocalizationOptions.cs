namespace Erp.Web.Localization;

public class LocalizationOptions
{
    public string DefaultCulture { get; set; } = "cs";
    public List<string> SupportedCultures { get; set; } = new();
}