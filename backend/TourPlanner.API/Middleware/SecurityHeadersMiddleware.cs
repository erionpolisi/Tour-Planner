using Microsoft.AspNetCore.Http;

namespace TourPlanner.API.Middleware;

/// <summary>
/// Adds a small, opinionated set of HTTP security response headers.
///
/// The values are aligned with the OWASP Secure Headers Project defaults and
/// with what NIST SP 800-53 / SP 800-63B expect from a web application that
/// protects an authenticator (JWT). Because this API returns JSON only (no
/// browsable HTML), the CSP is intentionally the strictest possible
/// (`default-src 'none'`).
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext ctx)
    {
        var h = ctx.Response.Headers;

        // Prevent MIME sniffing (protects against script injection via wrong content-type).
        h["X-Content-Type-Options"] = "nosniff";

        // Disallow embedding this API in an <iframe> (clickjacking / UI-redress protection).
        h["X-Frame-Options"] = "DENY";

        // Do not leak the API URL / query as a Referer to third parties.
        h["Referrer-Policy"] = "no-referrer";

        // Block Flash/Silverlight legacy XSS bypass.
        h["X-Permitted-Cross-Domain-Policies"] = "none";

        // Disable Feature/Permissions APIs that a JSON API never needs.
        h["Permissions-Policy"] =
            "accelerometer=(), autoplay=(), camera=(), geolocation=(), gyroscope=(), " +
            "magnetometer=(), microphone=(), payment=(), usb=()";

        // JSON-only API: nothing may load anything from anywhere. No inline, no scripts.
        h["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

        // Strict-Transport-Security is only meaningful on HTTPS. UseHsts adds it in prod;
        // we also add it here so it's present when the API is fronted by a TLS proxy.
        if (ctx.Request.IsHttps)
        {
            h["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        return _next(ctx);
    }
}
