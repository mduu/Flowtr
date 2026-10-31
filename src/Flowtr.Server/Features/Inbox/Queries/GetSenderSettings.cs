using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Server.Features.Inbox.Mail;
using Flowtr.Shared;
using Flowtr.Shared.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Flowtr.Server.Features.Inbox.Queries;

/// <summary>
/// The mailbox and who may send receipts to it, for the admin page.
/// </summary>
public sealed record GetSenderSettings;

public sealed class GetSenderSettingsHandler(ApplicationDbContext db, SenderCheck senders, MailboxState state, IOptions<MailboxOptions> options)
    : IQueryHandler<GetSenderSettings, SenderSettings>
{
    public async Task<Result<SenderSettings>> Handle(GetSenderSettings query, CancellationToken cancellationToken) =>
        Result.Success(new SenderSettings(
            GetInboxHandler.Status(options.Value, state),
            await senders.UserAddresses(cancellationToken),
            await db.AllowedSenders.AsNoTracking().OrderBy(s => s.Address).Select(s => new SenderView(s.Id, s.Address)).ToListAsync(cancellationToken)));
}
