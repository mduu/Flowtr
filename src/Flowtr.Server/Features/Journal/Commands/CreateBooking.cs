using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Server.Features.Journal.Model;
using Flowtr.Server.Features.Receipts;
using Flowtr.Shared;
using Flowtr.Shared.Journal;

namespace Flowtr.Server.Features.Journal.Commands;

/// <summary>
/// Saves a new booking. With <see cref="BookingInput.UploadIds"/>, its receipt gets the next number and its files are filed.
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

        var saved = await receipts.Save(booking, command.Input.UploadIds ?? [], [], cancellationToken);
        return saved.IsSuccess ? Result.Success(booking.Id) : Result.Failure<Guid>(saved.Error!);
    }
}
