using Flowtr.Shared;
using Flowtr.Shared.Inbox;

namespace Flowtr.Client.Features.Inbox;

/// <summary>
/// The inbox of receipts from mails (story #010) and, for admins, the allowed senders.
/// </summary>
public interface IInboxApi
{
    Task<Result<InboxView>> GetInbox(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the mailbox now. The AI reads the new receipts afterwards, in the background.
    /// </summary>
    Task<Result<FetchResult>> Fetch(CancellationToken cancellationToken = default);

    Task<Result> Discard(Guid id, CancellationToken cancellationToken = default);

    Task<Result<SenderSettings>> GetSenderSettings(CancellationToken cancellationToken = default);

    Task<Result<Guid>> AddSender(SenderInput input, CancellationToken cancellationToken = default);

    Task<Result> RemoveSender(Guid id, CancellationToken cancellationToken = default);
}
