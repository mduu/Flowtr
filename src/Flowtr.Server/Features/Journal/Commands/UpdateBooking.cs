using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Server.Features.Receipts;
using Flowtr.Shared;
using Flowtr.Shared.Journal;

namespace Flowtr.Server.Features.Journal.Commands;

/// <summary>
/// Changes a booking, and attaches, replaces or removes its receipt. Its old and its new year must be open.
/// </summary>
public sealed record UpdateBooking(Guid Id, BookingInput Input);

public sealed class UpdateBookingHandler(ApplicationDbContext db, BookingRules rules, ReceiptFiling receipts) : ICommandHandler<UpdateBooking, Result>
{
    public async Task<Result> Handle(UpdateBooking command, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.FindAsync([command.Id], cancellationToken);
        if (booking is null)
        {
            return Result.Failure("Diese Buchung gibt es nicht mehr.");
        }

        if (await rules.IsClosed(booking.Date.Year, cancellationToken))
        {
            return Result.Failure(string.Format(BookingRules.YearClosed, booking.Date.Year));
        }

        if ((await rules.Check(command.Input, booking.CategoryId, cancellationToken) ?? ReceiptFiling.CheckChange(booking, command.Input)) is { } error)
        {
            return Result.Failure(error);
        }

        // A new receipt in another year or kind needs a new number: the old receipt goes first.
        var removedBlob = command.Input.RemoveReceipt
            || (command.Input.UploadId is not null && (booking.Date.Year != command.Input.Date.Year || booking.Kind != command.Input.Kind))
                ? await receipts.Detach(booking, cancellationToken)
                : null;

        await rules.Apply(booking, command.Input, cancellationToken);

        if (command.Input.UploadId is { } uploadId)
        {
            var filed = await receipts.SaveWithReceipt(booking, uploadId, cancellationToken);
            if (!filed.IsSuccess)
            {
                return filed;
            }
        }
        else
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        await receipts.DeleteAfterSave(removedBlob, cancellationToken);
        return Result.Success();
    }
}
