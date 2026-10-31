using System.Net.Http.Json;
using Flowtr.Client.Api;
using Flowtr.Shared;
using Flowtr.Shared.Inbox;

namespace Flowtr.Client.Features.Inbox;

public sealed class HttpInboxApi(HttpClient http) : IInboxApi
{
    public Task<Result<InboxView>> GetInbox(CancellationToken cancellationToken = default) =>
        http.GetAsync("api/inbox", cancellationToken).Read<InboxView>(cancellationToken);

    public Task<Result<FetchResult>> Fetch(CancellationToken cancellationToken = default) =>
        http.PostAsync("api/inbox/fetch", null, cancellationToken).Read<FetchResult>(cancellationToken);

    public Task<Result> Discard(Guid id, CancellationToken cancellationToken = default) =>
        http.DeleteAsync($"api/inbox/{id}", cancellationToken).Read(cancellationToken);

    public Task<Result<SenderSettings>> GetSenderSettings(CancellationToken cancellationToken = default) =>
        http.GetAsync("api/inbox/senders", cancellationToken).Read<SenderSettings>(cancellationToken);

    public Task<Result<Guid>> AddSender(SenderInput input, CancellationToken cancellationToken = default) =>
        http.PostAsJsonAsync("api/inbox/senders", input, cancellationToken).Read<Guid>(cancellationToken);

    public Task<Result> RemoveSender(Guid id, CancellationToken cancellationToken = default) =>
        http.DeleteAsync($"api/inbox/senders/{id}", cancellationToken).Read(cancellationToken);
}
