using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Shared;

namespace Flowtr.Server.Features.Inbox.Commands;

public sealed record RemoveAllowedSender(Guid Id);

public sealed class RemoveAllowedSenderHandler(ApplicationDbContext db) : ICommandHandler<RemoveAllowedSender, Result>
{
    public async Task<Result> Handle(RemoveAllowedSender command, CancellationToken cancellationToken)
    {
        if (await db.AllowedSenders.FindAsync([command.Id], cancellationToken) is not { } sender)
        {
            return Result.Failure("Diesen Absender gibt es nicht mehr.");
        }

        db.AllowedSenders.Remove(sender);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
