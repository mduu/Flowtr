using Flowtr.Server.CrossCutting.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Flowtr.Tests.Hosting;

/// <summary>
/// Story #011: the gate together with the status code pages, as in <c>Program.cs</c>.
/// </summary>
public class StartupGatePipelineTests
{
    private readonly DatabaseReadiness readiness = new(TimeProvider.System);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Probe_of_a_bot_does_not_wake_the_database_through_the_not_found_page(bool loggedIn)
    {
        var context = await Send("/wp-admin/", loggedIn);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        StartRequested.ShouldBeFalse();
    }

    private bool StartRequested => readiness.WaitForStartRequest(CancellationToken.None).IsCompleted;

    private async Task<HttpContext> Send(string path, bool loggedIn)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(readiness)
            .BuildServiceProvider();
        var app = new ApplicationBuilder(services);
        app.UseStatusCodePagesWithReExecute("/not-found");
        app.UseMiddleware<StartupGate>();
        app.Run(context =>
        {
            // The real not-found page keeps the status code of the first request.
            if (context.Request.Path != "/not-found")
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
            }

            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = path;
        if (loggedIn)
        {
            context.Request.Headers.Cookie = ".AspNetCore.Identity.Application=cookie";
        }

        context.Items["csp-nonce"] = "test-nonce";
        context.Response.Body = new MemoryStream();
        await app.Build()(context);
        return context;
    }
}
