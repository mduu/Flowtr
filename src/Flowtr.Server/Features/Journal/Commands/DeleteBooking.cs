using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Shared;

namespace Flowtr.Server.Features.Journal.Commands;

/// <summary>
/// Deletes a booking of an open year. The audit log keeps its values.
/// </summary>
public sealed record DeleteBooking(Guid Id);

public sealed class DeleteBookingHandler(ApplicationDbContext db, BookingRules rules) : ICommandHandler<DeleteBooking, Result>
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

        db.Bookings.Remove(booking);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
