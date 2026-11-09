using Flowtr.Server.CrossCutting.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Flowtr.Tests.Hosting;

/// <summary>
/// Story #011: each response has the security headers (security.md, web security).
/// </summary>
public class SecurityHeadersTests
{
    [Fact]
    public async Task Response_has_the_security_headers()
    {
        // Act
        var context = await Send(Environments.Production);

        // Assert
        var headers = context.Response.Headers;
        headers.XContentTypeOptions.ToString().ShouldBe("nosniff");
        headers.XFrameOptions.ToString().ShouldBe("DENY");
        headers["Referrer-Policy"].ToString().ShouldBe("strict-origin-when-cross-origin");
        headers["Permissions-Policy"].ToString().ShouldContain("microphone=()");
        headers["X-Robots-Tag"].ToString().ShouldBe("noindex, nofollow");
        headers["Cross-Origin-Opener-Policy"].ToString().ShouldBe("same-origin");
    }

    [Fact]
    public async Task Policy_allows_only_own_scripts_and_the_nonce_of_the_request()
    {
        // Act
        var context = await Send(Environments.Production);

        // Assert
        var nonce = SecurityHeaders.Nonce(context).ShouldNotBeNull();
        var policy = context.Response.Headers.ContentSecurityPolicy.ToString();
        policy.ShouldContain($"script-src 'self' 'wasm-unsafe-eval' 'nonce-{nonce}'");
        policy.ShouldContain("frame-ancestors 'none'");
        policy.ShouldContain("object-src 'none'");
        policy.ShouldContain("connect-src 'self';");
        policy.ShouldContain("upgrade-insecure-requests");
        policy.ShouldNotContain("'unsafe-eval'");
    }

    [Fact]
    public async Task Each_request_gets_its_own_nonce()
    {
        var first = SecurityHeaders.Nonce(await Send(Environments.Production));
        var second = SecurityHeaders.Nonce(await Send(Environments.Production));

        first.ShouldNotBe(second);
    }

    [Fact]
    public async Task Hot_reload_may_connect_in_development()
    {
        var context = await Send(Environments.Development);

        context.Response.Headers.ContentSecurityPolicy.ToString().ShouldContain("connect-src 'self' ws: wss:");
    }

    [Fact]
    public async Task Endpoint_can_set_a_stricter_policy()
    {
        // The view of a mail (ADR-009) sets its own policy with sandbox.
        var context = await Send(Environments.Production, endpoint: http => http.Response.Headers.ContentSecurityPolicy = "sandbox; default-src 'none'");

        context.Response.Headers.ContentSecurityPolicy.ToString().ShouldBe("sandbox; default-src 'none'");
    }

    private static async Task<HttpContext> Send(string environment, Action<HttpContext>? endpoint = null)
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeaders(http =>
        {
            endpoint?.Invoke(http);
            return Task.CompletedTask;
        }, new Environment(environment));

        await middleware.InvokeAsync(context);
        return context;
    }

    private sealed class Environment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;

        public string ApplicationName { get; set; } = "Flowtr.Server";

        public string ContentRootPath { get; set; } = "";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
