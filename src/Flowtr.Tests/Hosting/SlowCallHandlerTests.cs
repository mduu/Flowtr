using System.Net;
using System.Net.Http.Json;
using Flowtr.Client.Features.Status;
using Flowtr.Shared.Status;

namespace Flowtr.Tests.Hosting;

/// <summary>
/// Story #011: a slow call shows «Die Datenbank wacht auf …» only when the server says so.
/// </summary>
public class SlowCallHandlerTests
{
    private readonly ServerStatus status = new();
    private readonly TaskCompletionSource<HttpResponseMessage> answer = new();

    [Fact]
    public async Task Slow_call_shows_the_hint_while_the_database_wakes()
    {
        // Arrange
        var client = Client(waking: true);

        // Act
        var call = client.GetAsync("/api/journal");
        await WaitFor(() => status.IsWaking);
        answer.SetResult(new HttpResponseMessage(HttpStatusCode.OK));
        await call;

        // Assert
        status.IsWaking.ShouldBeFalse();
    }

    [Fact]
    public async Task Slow_call_for_another_reason_shows_nothing()
    {
        // Arrange
        var client = Client(waking: false);
        var shown = false;
        status.Changed += () => shown = true;

        // Act
        var call = client.PostAsync("/api/receipts/analyze", null);
        await Task.Delay(SlowCallHandler.Patience + TimeSpan.FromMilliseconds(500));
        answer.SetResult(new HttpResponseMessage(HttpStatusCode.OK));
        await call;

        // Assert
        shown.ShouldBeFalse();
    }

    private HttpClient Client(bool waking) =>
        new(new SlowCallHandler(status) { InnerHandler = new Server(answer, waking) }) { BaseAddress = new Uri("https://flowtr.test/") };

    private static async Task WaitFor(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
        {
            await Task.Delay(50);
        }

        condition().ShouldBeTrue();
    }

    private sealed class Server(TaskCompletionSource<HttpResponseMessage> answer, bool waking) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            request.RequestUri!.AbsolutePath == "/" + StatusView.Path
                ? Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new StatusView(true, waking)) })
                : answer.Task;
    }
}
