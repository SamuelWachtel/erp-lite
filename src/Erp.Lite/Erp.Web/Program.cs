using System.Globalization;
using MudBlazor.Services;
using Erp.Web.Components;
using Erp.Web.Localization;
using Microsoft.AspNetCore.Localization;
using MudBlazor;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();

builder.Services.AddLocalization(options => options.ResourcesPath = "Localization");

builder.Services.AddScoped<LocalizationCache>(sp =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var culture = httpContextAccessor.HttpContext?.Features.Get<IRequestCultureFeature>()?.RequestCulture?.UICulture
                  ?? CultureInfo.CurrentUICulture;

    return new LocalizationCache(culture);
});

builder.Services.Configure<LocalizationOptions>(
    builder.Configuration.GetSection("Localization"));

var localizationOptions = builder.Configuration
    .GetSection("Localization")
    .Get<LocalizationOptions>();

var requestLocalizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(localizationOptions.DefaultCulture)
    .AddSupportedCultures(localizationOptions.SupportedCultures.ToArray())
    .AddSupportedUICultures(localizationOptions.SupportedCultures.ToArray());

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 1000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});

var app = builder.Build();

app.UseCultureRedirect();

app.UseRequestLocalization(requestLocalizationOptions);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
