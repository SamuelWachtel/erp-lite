using System.Globalization;
using Microsoft.Extensions.Options;

namespace Erp.Web.Localization
{
    public class LocalizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly LocalizationOptions _localizationOptions;

        public LocalizationMiddleware(RequestDelegate next, IOptions<LocalizationOptions> options)
        {
            _next = next;
            _localizationOptions = options.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // ✅ Skip processing for POSTs and OIDC
            if (!HttpMethods.IsGet(context.Request.Method) ||
                path.StartsWith("/signin-oidc", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/connect", StringComparison.OrdinalIgnoreCase)) // For OpenIddict endpoints
            {
                await _next(context);
                return;
            }

            if (path.Length > 1 && path.EndsWith("/"))
            {
                context.Response.Redirect(path.TrimEnd('/'), false);
                return;
            }

            if (path.StartsWith("/_framework") ||
                path.StartsWith("/signin-oidc") ||
                path.StartsWith("/connect") ||
                path.StartsWith("/_blazor") ||
                path.StartsWith("/login") ||
                path.StartsWith("/logout") ||
                path.StartsWith("/_content") ||
                path.StartsWith("/favicon.ico") ||
                path.StartsWith("/css") ||
                path.StartsWith("/js"))
            {
                await _next(context);
                return;
            }

            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                context.Response.Redirect("/" + _localizationOptions.DefaultCulture, false);
                return;
            }

            var cultureCandidate = segments[0].ToLower();

            if (_localizationOptions.SupportedCultures.Contains(cultureCandidate))
            {
                var mappedCulture = cultureCandidate switch
                {
                    "cz" => "cs-CZ",
                    "en" => "en-US",
                    _ => _localizationOptions.DefaultCulture
                };
                
                var cultureInfo = new CultureInfo(mappedCulture);
                CultureInfo.CurrentCulture = cultureInfo;
                CultureInfo.CurrentUICulture = cultureInfo;

                await _next(context);
                return;
            }

            if (cultureCandidate.Length == 2 && !_localizationOptions.SupportedCultures.Contains(cultureCandidate))
            {
                var restOfPath = string.Join('/', segments.Skip(1));
                var fixedPath = "/" + _localizationOptions.DefaultCulture +
                                (string.IsNullOrEmpty(restOfPath) ? "" : "/" + restOfPath);

                if (!context.Request.Path.Value.Equals(fixedPath, StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Redirect(fixedPath, false);
                    return;
                }
            }

            var newPath = "/" + _localizationOptions.DefaultCulture + path;
            context.Response.Redirect(newPath, false);
        }
    }
}