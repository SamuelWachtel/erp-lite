using System.Globalization;
using MudBlazor.Services;
using Erp.Web.Components;
using Erp.Web.Extensions;
using Erp.Web.Localization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
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

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
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
