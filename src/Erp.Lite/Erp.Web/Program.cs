using System.Globalization;
using MudBlazor.Services;
using Erp.Web.Components;
using Erp.Web.Config;
using Erp.Web.Extensions;
using Erp.Web.Localization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.WebUtilities;
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

var oidcSettingsSection = builder.Configuration.GetSection("Authentication");
builder.Services.Configure<OidcSettings>(oidcSettingsSection);
var oidcSettings = oidcSettingsSection.Get<OidcSettings>()!;

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(IdentityConstants.ApplicationScheme)
    .AddOpenIdConnect(options =>
    {
        options.Authority = oidcSettings.Authority;
        options.ClientId = oidcSettings.ClientId;
        options.ResponseType = oidcSettings.ResponseType;
        options.RequireHttpsMetadata = oidcSettings.RequireHttpsMetadata;

        options.SaveTokens = oidcSettings.SaveTokens;

        options.CallbackPath = new PathString(new Uri(oidcSettings.RedirectUri).AbsolutePath);
        options.SignedOutCallbackPath = new PathString(new Uri(oidcSettings.PostLogoutRedirectUri).AbsolutePath);

        options.UsePkce = true;

        options.Scope.Clear();
        foreach (var scope in oidcSettings.Scopes)
        {
            options.Scope.Add(scope);
        }

        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "role";
        
        if (builder.Environment.IsDevelopment())
        {
            options.BackchannelHttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
        }
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

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

builder.Services.AddHttpClient("myClient")
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        if (builder.Environment.IsDevelopment())
        {
            return new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
        }

        return new HttpClientHandler();
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
        RedirectUri = "/"
    });
});

app.MapGet("logout", async context =>
{
    var postLogoutRedirectUri = "https://localhost:7104/login";

    var logoutUrl = QueryHelpers.AddQueryString("https://localhost/connect/logout", new Dictionary<string, string?>
    {
        ["post_logout_redirect_uri"] = postLogoutRedirectUri
    });

    context.Response.Redirect(logoutUrl);
    await Task.CompletedTask;
});

app.Run();
