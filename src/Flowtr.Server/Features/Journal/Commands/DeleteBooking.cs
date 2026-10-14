using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Server.Features.Receipts;
using Flowtr.Shared;

namespace Flowtr.Server.Features.Journal.Commands;

/// <summary>
/// Deletes a booking of an open year, with its receipt. The audit log keeps the values of both.
/// </summary>
public sealed record DeleteBooking(Guid Id);

public sealed class DeleteBookingHandler(ApplicationDbContext db, BookingRules rules, ReceiptFiling receipts) : ICommandHandler<DeleteBooking, Result>
{
    public async Task<Result> Handle(DeleteBooking command, CancellationToken cancellationToken)
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

        var removedBlob = await receipts.Detach(booking, cancellationToken);
        db.Bookings.Remove(booking);
        await db.SaveChangesAsync(cancellationToken);

        await receipts.DeleteAfterSave(removedBlob, cancellationToken);
        return Result.Success();
    }
}
