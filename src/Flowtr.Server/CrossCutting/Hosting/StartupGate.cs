using System.Net;
using Flowtr.Shared.Status;

namespace Flowtr.Server.CrossCutting.Hosting;

/// <summary>
/// Wakes the database when a request needs it, and holds requests back until it is ready (story #011). A page shows «Flowtr startet …» and loads again
/// when Flowtr is ready. An API call or a form waits: the client shows a hint while it waits.
/// </summary>
/// <remarks>
/// It also answers <c>GET /status</c>, before the authentication: the authentication checks the login cookie
/// against the database, and the status must answer while the database wakes up.
/// </remarks>
public sealed class StartupGate(RequestDelegate next, DatabaseReadiness readiness)
{
    /// <summary>
    /// How long a request waits for the database. Azure SQL resumes in about a minute.
    /// </summary>
    public static readonly TimeSpan MaxWait = TimeSpan.FromMinutes(2);

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.Equals("/" + StatusView.Path, StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Headers.CacheControl = "no-store";
            await context.Response.WriteAsJsonAsync(readiness.Status);
            return;
        }

        if (IsProbe(context.Request.Path))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (readiness.IsReady || NeedsNoDatabase(context.Request.Path) || IsLoggedOutPage(context.Request))
        {
            await next(context);
            return;
        }

        readiness.RequestStart();
        if (IsPage(context.Request))
        {
            await WriteStartingPage(context);
            return;
        }

        if (await readiness.WaitUntilReady(MaxWait, context.RequestAborted))
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsync("Die Datenbank ist nicht erreichbar. Bitte versuche es später wieder.");
    }

    private static bool NeedsNoDatabase(PathString path) =>
        path.StartsWithSegments("/_framework")
        || path.StartsWithSegments("/_content")
        || Path.HasExtension(path.Value);

    /// <summary>
    /// Bots and scanners look for known weak points. Flowtr has none of these paths.
    /// </summary>
    public static bool IsProbe(PathString path)
    {
        var value = path.Value ?? "";
        var segments = value.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(s => s.StartsWith('.') && !s.Equals(".well-known", StringComparison.OrdinalIgnoreCase))
            || segments.Any(s => s.StartsWith("wp-", StringComparison.OrdinalIgnoreCase)
                || s.Equals("cgi-bin", StringComparison.OrdinalIgnoreCase)
                || s.Equals("phpmyadmin", StringComparison.OrdinalIgnoreCase))
            || ProbeExtensions.Contains(Path.GetExtension(value));
    }

    private static readonly HashSet<string> ProbeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".php", ".asp", ".aspx", ".jsp", ".cgi", ".env", ".git", ".bak", ".sql",
    };

    /// <summary>
    /// Without a login, a page needs no database: it goes to the login page. The other account pages need it,
    /// for example an invitation.
    /// </summary>
    private static bool IsLoggedOutPage(HttpRequest request) =>
        IsPage(request)
        && !request.Cookies.ContainsKey(LoginCookie)
        && (!request.Path.StartsWithSegments("/Account") || request.Path.StartsWithSegments("/Account/Login"));

    private const string LoginCookie = ".AspNetCore.Identity.Application";

    private static bool IsPage(HttpRequest request) =>
        (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method))
        && !request.Path.StartsWithSegments("/api");

    private static Task WriteStartingPage(HttpContext context)
    {
        var response = context.Response;
        response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        response.Headers.RetryAfter = "5";
        response.Headers.CacheControl = "no-store";
        response.ContentType = "text/html; charset=utf-8";
        return response.WriteAsync(StartingPage(SecurityHeaders.Nonce(context) ?? ""));
    }

    /// <summary>
    /// Without Blazor and without MudBlazor: they need the server, and the server waits for the database.
    /// </summary>
    public static string StartingPage(string nonce) => $$"""
        <!DOCTYPE html>
        <html lang="de-CH">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <base href="/" />
            <title>Flowtr startet …</title>
            <link rel="icon" type="image/svg+xml" href="favicon.svg" />
            <link href="https://fonts.googleapis.com/css2?family=Roboto:wght@400;500&display=swap" rel="stylesheet" />
            <style>
                /* The colours of the MudBlazor theme (FlowtrTheme): MudBlazor is not loaded here. */
                :root { color-scheme: light dark; --primary: #3f51b5; --text: rgba(0,0,0,.87); --muted: rgba(0,0,0,.54); --track: #c5cae9; --bg: #fff; }
                @media (prefers-color-scheme: dark) {
                    :root { --primary: #7986cb; --text: rgba(255,255,255,.87); --muted: rgba(255,255,255,.6); --track: #3c3f5c; --bg: #32333d; }
                }
                body { margin: 0; min-height: 100vh; display: grid; place-items: center; background: var(--bg); color: var(--text);
                       font-family: Roboto, -apple-system, "Segoe UI", sans-serif; }
                main { width: min(360px, calc(100vw - 32px)); text-align: center; }
                img { height: 40px; margin-bottom: 24px; }
                h1 { font-size: 1.25rem; font-weight: 500; margin: 0 0 8px; }
                p { margin: 0; line-height: 1.5; }
                .muted { color: var(--muted); font-size: .875rem; margin-top: 16px; }
                .bar { position: relative; height: 4px; margin: 24px 0 0; overflow: hidden; border-radius: 2px; background: var(--track); }
                .bar::after { content: ""; position: absolute; inset: 0 auto 0 0; width: 40%; background: var(--primary); animation: run 1.4s ease-in-out infinite; }
                @keyframes run { from { left: -40%; } to { left: 100%; } }
                @media (prefers-reduced-motion: reduce) { .bar::after { animation-duration: 4s; } }
            </style>
        </head>
        <body>
            <main role="status" aria-live="polite">
                <picture>
                    <source srcset="logo-dark.svg" media="(prefers-color-scheme: dark)" />
                    <img src="logo.svg" alt="Flowtr" />
                </picture>
                <h1>Flowtr startet …</h1>
                <p>Die Datenbank wacht auf. Das dauert bis zu einer Minute.</p>
                <div class="bar" aria-hidden="true"></div>
                <p class="muted">Die Seite lädt von selbst neu.</p>
            </main>
            <script nonce="{{WebUtility.HtmlEncode(nonce)}}">
                (function poll() {
                    fetch('/{{StatusView.Path}}', { cache: 'no-store' })
                        .then(r => r.json())
                        .then(s => s.ready ? location.reload() : setTimeout(poll, 2000))
                        .catch(() => setTimeout(poll, 2000));
                })();
            </script>
        </body>
        </html>
        """;
}
