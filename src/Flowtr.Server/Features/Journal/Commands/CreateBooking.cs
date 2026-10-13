using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Server.Features.Journal.Model;
using Flowtr.Shared;
using Flowtr.Shared.Journal;

namespace Flowtr.Server.Features.Journal.Commands;

public sealed record CreateBooking(BookingInput Input);

public sealed class CreateBookingHandler(ApplicationDbContext db, BookingRules rules, TimeProvider time)
    : ICommandHandler<CreateBooking, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateBooking command, CancellationToken cancellationToken)
    {
        if (await rules.Check(command.Input, currentCategoryId: null, cancellationToken) is { } error)
        {
            return Result.Failure<Guid>(error);
        }

        var booking = new Booking { CreatedAt = time.GetUtcNow() };
        await rules.Apply(booking, command.Input, cancellationToken);
        db.Bookings.Add(booking);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(booking.Id);
    }
}
