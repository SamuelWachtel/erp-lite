using System.Globalization;

namespace Erp.Web.Localization;

public class LocalizationCache
{
    private CultureInfo _culture;

    public LocalizationCache(CultureInfo culture)
    {
        _culture = culture;
    }

    public void SetCulture(CultureInfo culture)
    {
        _culture = culture;
    }

    public string Welcome => Resources.ResourceManager.GetString("Welcome", _culture) ?? string.Empty;
    public string Logout => Resources.ResourceManager.GetString("Logout", _culture) ?? string.Empty;
}