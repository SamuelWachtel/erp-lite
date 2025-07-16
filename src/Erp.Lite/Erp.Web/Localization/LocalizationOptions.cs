namespace Erp.Web.Localization;

public class LocalizationOptions
{
    public string DefaultCulture { get; set; } = "cz";
    public List<string> SupportedCultures { get; set; } = new();
}