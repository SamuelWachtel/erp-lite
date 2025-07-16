using System.Globalization;
using MudBlazor.Services;
using Erp.Web.Components;
using Erp.Web.Extensions;
using Erp.Web.Localization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMudServices();

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

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/login", async context =>
{
    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
    {
        RedirectUri = "/" // redirect after successful login
    });
});

app.MapGet("logout", async context =>
{
    // Redirect user to the identity provider's logout endpoint with post logout redirect URI
    var postLogoutRedirectUri = "https://localhost:7104/signout-callback-oidc"; // your client app URL after logout

    var logoutUrl = QueryHelpers.AddQueryString("https://localhost:7056/connect/logout", new Dictionary<string, string?>
    {
        ["post_logout_redirect_uri"] = postLogoutRedirectUri
    });

    context.Response.Redirect(logoutUrl);
    await Task.CompletedTask;
});

app.Run();
