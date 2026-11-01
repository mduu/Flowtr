using System.Net.Http.Json;
using Flowtr.Shared.Status;

namespace Flowtr.Client.Features.Status;

/// <summary>
/// When a call to the server takes long, asks the server whether the database wakes up, and tells <see cref="ServerStatus"/>.
/// A slow call for another reason (for example the AI) shows nothing.
/// </summary>
public sealed class SlowCallHandler(ServerStatus status) : DelegatingHandler
{
    public static readonly TimeSpan Patience = TimeSpan.FromSeconds(2);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var call = base.SendAsync(request, cancellationToken);
        IDisposable? waking = null;
        try
        {
            while (await Task.WhenAny(call, Task.Delay(Patience, cancellationToken)) != call)
            {
                if (waking is null && await DatabaseWakes(request.RequestUri!, cancellationToken))
                {
                    waking = status.Waking();
                }
            }

            return await call;
        }
        finally
        {
            waking?.Dispose();
        }
    }

    private async Task<bool> DatabaseWakes(Uri callUri, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(callUri, "/" + StatusView.Path));
            using var response = await base.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode
                && await response.Content.ReadFromJsonAsync<StatusView>(cancellationToken) is { Waking: true };
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }
}
