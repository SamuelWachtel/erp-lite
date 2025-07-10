using Microsoft.Extensions.Options;
using Erp.Web.Localization;

namespace Erp.Web.Extensions
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

            if (path.Length > 1 && path.EndsWith("/"))
            {
                context.Response.Redirect(path.TrimEnd('/'), false);
                return;
            }

            if (path.StartsWith("/_framework") ||
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
                await _next(context);
                return;
            }

            if (cultureCandidate.Length == 2 && cultureCandidate.All(char.IsLetter))
            {
                var restOfPath = string.Join('/', segments.Skip(1));
                var fixedPath = "/" + _localizationOptions.DefaultCulture + (string.IsNullOrEmpty(restOfPath) ? "" : "/" + restOfPath);
                context.Response.Redirect(fixedPath, false);
                return;
            }

            var newPath = "/" + _localizationOptions.DefaultCulture + path;
            context.Response.Redirect(newPath, false);
        }
    }
}
