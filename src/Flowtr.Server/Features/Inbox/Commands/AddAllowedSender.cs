using System.Text.RegularExpressions;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Server.Features.Inbox.Model;
using Flowtr.Shared;
using Flowtr.Shared.Inbox;
using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.Features.Inbox.Commands;

/// <summary>
/// Allows an address (<c>rechnungen@example.ch</c>) or a domain (<c>@example.ch</c>) to send receipts.
/// </summary>
public sealed record AddAllowedSender(SenderInput Input);

public sealed partial class AddAllowedSenderHandler(ApplicationDbContext db) : ICommandHandler<AddAllowedSender, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddAllowedSender command, CancellationToken cancellationToken)
    {
        var address = command.Input.Address?.Trim().ToLowerInvariant() ?? "";
        if (!AddressOrDomain().IsMatch(address) || address.Length > 256)
        {
            return Result.Failure<Guid>("Bitte eine Adresse (name@firma.ch) oder eine Domain (@firma.ch) angeben.");
        }

        if (await db.AllowedSenders.AnyAsync(s => s.Address == address, cancellationToken))
        {
            return Result.Failure<Guid>($"{address} ist schon erlaubt.");
        }

        var sender = new AllowedSender { Address = address };
        db.AllowedSenders.Add(sender);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(sender.Id);
    }

    [GeneratedRegex(@"^[^@\s]*@[^@\s.]+(\.[^@\s.]+)+$")]
    private static partial Regex AddressOrDomain();
}
