using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Server.Features.Receipts;
using Flowtr.Shared;
using Flowtr.Shared.Journal;

namespace Flowtr.Server.Features.Journal.Commands;

/// <summary>
/// Changes a booking, and adds or removes files of its receipt. Its old and its new year must be open.
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

        if ((await rules.Check(command.Input, booking.CategoryId, cancellationToken)
             ?? await receipts.CheckChange(booking, command.Input, cancellationToken)) is { } error)
        {
            return Result.Failure(error);
        }

        await rules.Apply(booking, command.Input, cancellationToken);
        return await receipts.Save(booking, command.Input.UploadIds ?? [], command.Input.RemovedFileIds ?? [], cancellationToken);
    }
}
