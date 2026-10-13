using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Shared;
using Flowtr.Shared.Journal;
using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.Features.Journal.Queries;

/// <summary>
/// Lists for the booking form: all categories in their order (inactive ones for old bookings), counterparties and payment methods.
/// </summary>
public sealed record GetBookingOptions;

public sealed class GetBookingOptionsHandler(ApplicationDbContext db, IQueryHandler<GetCategories, IReadOnlyList<CategoryView>> categories)
    : IQueryHandler<GetBookingOptions, BookingOptions>
{
    public async Task<Result<BookingOptions>> Handle(GetBookingOptions query, CancellationToken cancellationToken)
    {
        var allCategories = await categories.Handle(new GetCategories(), cancellationToken);

        return Result.Success(new BookingOptions(
            allCategories.Value!,
            await db.Counterparties.OrderBy(c => c.Name).Select(c => c.Name).ToListAsync(cancellationToken),
            await db.PaymentMethods.OrderBy(m => m.Name).Select(m => m.Name).ToListAsync(cancellationToken)));
    }
}
