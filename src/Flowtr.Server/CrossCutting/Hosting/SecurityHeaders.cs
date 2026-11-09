using System.Security.Cryptography;

namespace Flowtr.Server.CrossCutting.Hosting;

/// <summary>
/// Security headers on each response (security.md, web security). An endpoint can set a stricter
/// Content-Security-Policy, for example the view of a mail (ADR-009): it replaces this one.
/// </summary>
/// <remarks>
/// Blazor WebAssembly needs <c>'wasm-unsafe-eval'</c>. The inline import map of Blazor gets a nonce per request.
/// MudBlazor sets inline styles. Fonts come from Google Fonts.
/// </remarks>
public sealed class SecurityHeaders(RequestDelegate next, IHostEnvironment environment)
{
    private const string NonceKey = "csp-nonce";

    public Task InvokeAsync(HttpContext context)
    {
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        context.Items[NonceKey] = nonce;

        var headers = context.Response.Headers;
        headers.ContentSecurityPolicy = Policy(nonce, environment.IsDevelopment());
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(self), microphone=(), geolocation=(), payment=(), usb=()";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        // A personal app: no search engine shows it (also robots.txt).
        headers["X-Robots-Tag"] = "noindex, nofollow";

        return next(context);
    }

    /// <summary>
    /// The nonce of this request for an inline script, for example the import map.
    /// </summary>
    public static string? Nonce(HttpContext context) => context.Items[NonceKey] as string;

    /// <param name="development">Hot reload in development connects with a WebSocket.</param>
    public static string Policy(string nonce, bool development) => string.Join("; ",
    [
        "default-src 'self'",
        $"script-src 'self' 'wasm-unsafe-eval' 'nonce-{nonce}'",
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com",
        "font-src 'self' https://fonts.gstatic.com",
        "img-src 'self' data:",
        development ? "connect-src 'self' ws: wss:" : "connect-src 'self'",
        "object-src 'none'",
        "base-uri 'self'",
        "form-action 'self'",
        "frame-ancestors 'none'",
        .. development ? Array.Empty<string>() : ["upgrade-insecure-requests"],
    ]);
}
