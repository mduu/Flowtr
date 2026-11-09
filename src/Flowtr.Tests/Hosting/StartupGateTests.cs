using System.Text;
using Flowtr.Server.CrossCutting.Hosting;
using Microsoft.AspNetCore.Http;

namespace Flowtr.Tests.Hosting;

/// <summary>
/// Story #011: until the database is ready, a page shows «Flowtr startet …» and other requests wait.
/// Only a request that needs the database wakes it up.
/// </summary>
public class StartupGateTests
{
    private readonly DatabaseReadiness readiness = new(TimeProvider.System);
    private bool passed;

    [Fact]
    public async Task Page_shows_that_Flowtr_starts()
    {
        // Act
        var context = await Send("GET", "/journal", loggedIn: true);

        // Assert
        passed.ShouldBeFalse();
        StartRequested.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
        context.Response.Headers.RetryAfter.ToString().ShouldBe("5");
        Body(context).ShouldContain("Flowtr startet");
        Body(context).ShouldContain("<script nonce=\"test-nonce\">");
    }

    [Fact]
    public async Task Status_answers_while_the_database_wakes()
    {
        // Act
        var context = await Send("GET", "/status");

        // Assert
        passed.ShouldBeFalse();
        Body(context).ShouldBe("""{"ready":false,"waking":true}""");
        StartRequested.ShouldBeFalse();
    }

    [Theory]
    [InlineData("/journal")]
    [InlineData("/Account/Login")]
    public async Task Page_without_login_passes_and_the_database_sleeps(string path)
    {
        await Send("GET", path);

        (passed, StartRequested).ShouldBe((true, false));
    }

    [Fact]
    public async Task Account_page_without_login_wakes_the_database()
    {
        await Send("GET", "/Account/AcceptInvitation");

        (passed, StartRequested).ShouldBe((false, true));
    }

    [Theory]
    [InlineData("/wp-admin/setup-config.php")]
    [InlineData("/.env")]
    [InlineData("/.git/config")]
    [InlineData("/cgi-bin/luci")]
    [InlineData("/index.php")]
    public async Task Probe_of_a_bot_gets_not_found_and_the_database_sleeps(string path)
    {
        var context = await Send("GET", path, loggedIn: true);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        (passed, StartRequested).ShouldBe((false, false));
    }

    [Fact]
    public void Well_known_path_is_no_probe() => StartupGate.IsProbe("/.well-known/passkey-endpoints").ShouldBeFalse();

    [Theory]
    [InlineData("/_framework/blazor.web.js")]
    [InlineData("/logo.svg")]
    public async Task Files_pass_without_the_database(string path)
    {
        await Send("GET", path, loggedIn: true);

        (passed, StartRequested).ShouldBe((true, false));
    }

    [Fact]
    public async Task Api_call_waits_until_the_database_is_ready()
    {
        // Act
        var sending = Send("GET", "/api/journal");
        await Task.Delay(50);
        var passedBefore = passed;
        readiness.MarkReady();
        await sending;

        // Assert
        (passedBefore, passed).ShouldBe((false, true));
        StartRequested.ShouldBeTrue();
    }

    [Fact]
    public async Task Form_waits_until_the_database_is_ready()
    {
        var sending = Send("POST", "/Account/Login");
        readiness.MarkReady();
        await sending;

        passed.ShouldBeTrue();
    }

    [Fact]
    public async Task Everything_passes_when_the_database_is_ready()
    {
        readiness.MarkReady();

        await Send("GET", "/journal");

        passed.ShouldBeTrue();
    }

    private bool StartRequested => readiness.WaitForStartRequest(CancellationToken.None).IsCompleted;

    private async Task<HttpContext> Send(string method, string path, bool loggedIn = false)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        if (loggedIn)
        {
            context.Request.Headers.Cookie = ".AspNetCore.Identity.Application=cookie";
        }

        context.Items["csp-nonce"] = "test-nonce";
        context.Response.Body = new MemoryStream();
        var gate = new StartupGate(_ =>
        {
            passed = true;
            return Task.CompletedTask;
        }, readiness);

        await gate.InvokeAsync(context);
        return context;
    }

    private static string Body(HttpContext context) => Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray());
}
