namespace Erp.Web.Localization;

public static class LocalizationMiddlewareExtension
{
    public static IApplicationBuilder UseCultureRedirect(this IApplicationBuilder app)
    {
        return app.UseMiddleware<LocalizationMiddleware>();
    }
}