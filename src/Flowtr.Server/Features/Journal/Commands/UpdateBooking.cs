using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Shared;
using Flowtr.Shared.Journal;

namespace Flowtr.Server.Features.Journal.Commands;

/// <summary>
/// Changes a booking. Its old and its new year must be open.
/// </summary>
public sealed record UpdateBooking(Guid Id, BookingInput Input);

public sealed class UpdateBookingHandler(ApplicationDbContext db, BookingRules rules) : ICommandHandler<UpdateBooking, Result>
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

        if (await rules.Check(command.Input, booking.CategoryId, cancellationToken) is { } error)
        {
            return Result.Failure(error);
        }

        await rules.Apply(booking, command.Input, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
