using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace KazanlakEvents.Web.Middleware;

public class SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment env)
{
    private const string CspProduction =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' cdn.jsdelivr.net cdnjs.cloudflare.com; " +
        "style-src 'self' 'unsafe-inline' cdn.jsdelivr.net fonts.googleapis.com; " +
        "font-src 'self' fonts.gstatic.com cdn.jsdelivr.net; " +
        "img-src 'self' data: blob: *.tile.openstreetmap.org images.unsplash.com cdn.jsdelivr.net; " +
        "connect-src 'self' ws://localhost:* http://localhost:* https://cdn.jsdelivr.net; " +
        "frame-src 'self' https://js.stripe.com; " +
        "media-src 'self';";

    private const string CspDevelopment =
        "default-src 'self' 'unsafe-inline' 'unsafe-eval' *; " +
        "img-src * data: blob:; " +
        "font-src *; " +
        "style-src * 'unsafe-inline'; " +
        "connect-src *;";

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"]  = "nosniff";
        headers["X-Frame-Options"]          = "DENY";
        headers["X-XSS-Protection"]         = "1; mode=block";
        headers["Referrer-Policy"]          = "strict-origin-when-cross-origin";
        headers["Content-Security-Policy"]  = env.IsDevelopment() ? CspDevelopment : CspProduction;
        headers["Permissions-Policy"]       = "camera=(self), microphone=(), geolocation=(self)";

        await next(context);
    }
}
