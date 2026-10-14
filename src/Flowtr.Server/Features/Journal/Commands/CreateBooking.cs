using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Server.Features.Journal.Model;
using Flowtr.Server.Features.Receipts;
using Flowtr.Shared;
using Flowtr.Shared.Journal;

namespace Flowtr.Server.Features.Journal.Commands;

/// <summary>
/// Saves a new booking. With <see cref="BookingInput.UploadId"/>, its receipt gets the next number and is filed.
/// </summary>
public sealed record CreateBooking(BookingInput Input);

public sealed class CreateBookingHandler(ApplicationDbContext db, BookingRules rules, ReceiptFiling receipts, TimeProvider time)
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

        if (command.Input.UploadId is { } uploadId)
        {
            var filed = await receipts.SaveWithReceipt(booking, uploadId, cancellationToken);
            return filed.IsSuccess ? Result.Success(booking.Id) : Result.Failure<Guid>(filed.Error!);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(booking.Id);
    }
}
