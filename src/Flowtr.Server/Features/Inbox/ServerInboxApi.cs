using Flowtr.Client.Features.Inbox;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Features.Inbox.Queries;
using Flowtr.Shared;
using Flowtr.Shared.Inbox;
using Flowtr.Shared.Users;

namespace Flowtr.Server.Features.Inbox;

/// <summary>
/// <see cref="IInboxApi"/> for prerendering: only the reads. The changes happen in the browser, after a user action.
/// </summary>
public sealed class ServerInboxApi(IServiceProvider services, IHttpContextAccessor httpContextAccessor) : IInboxApi
{
    private const string NotAllowed = "Dafür hast du keine Berechtigung.";

    private const string BrowserOnly = "Das geht nur im Browser.";

    public Task<Result<InboxView>> GetInbox(CancellationToken cancellationToken = default) =>
        Roles.All.Any(role => User?.IsInRole(role) == true)
            ? services.GetRequiredService<IQueryHandler<GetInbox, InboxView>>().Handle(new GetInbox(), cancellationToken)
            : Task.FromResult(Result.Failure<InboxView>(NotAllowed));

    public Task<Result<SenderSettings>> GetSenderSettings(CancellationToken cancellationToken = default) =>
        User?.IsInRole(Roles.Admin) == true
            ? services.GetRequiredService<IQueryHandler<GetSenderSettings, SenderSettings>>().Handle(new GetSenderSettings(), cancellationToken)
            : Task.FromResult(Result.Failure<SenderSettings>(NotAllowed));

    public Task<Result<FetchResult>> Fetch(CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure<FetchResult>(BrowserOnly));

    public Task<Result> Discard(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure(BrowserOnly));

    public Task<Result<Guid>> AddSender(SenderInput input, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure<Guid>(BrowserOnly));

    public Task<Result> RemoveSender(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure(BrowserOnly));

    private System.Security.Claims.ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;
}
