using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Shared;
using Flowtr.Shared.Journal;
using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.Features.Journal.Queries;

/// <summary>
/// The bookings of a year for a filter, newest first, with the totals of the whole year.
/// </summary>
public sealed record GetJournal(JournalFilter Filter);

public sealed class GetJournalHandler(ApplicationDbContext db, TimeProvider time) : IQueryHandler<GetJournal, JournalView>
{
    public async Task<Result<JournalView>> Handle(GetJournal query, CancellationToken cancellationToken)
    {
        var filter = query.Filter;
        var start = new DateOnly(filter.Year, 1, 1);
        var end = start.AddYears(1);

        var year = db.Bookings.AsNoTracking().Where(b => b.Date >= start && b.Date < end);
        var income = await year.Where(b => b.Kind == BookingKind.Income).SumAsync(b => b.AmountChf, cancellationToken);
        var expense = await year.Where(b => b.Kind == BookingKind.Expense).SumAsync(b => b.AmountChf, cancellationToken);

        var bookings = year;

        if (filter.Kind is { } kind)
        {
            bookings = bookings.Where(b => b.Kind == kind);
        }

        if (filter.CategoryId is { } categoryId)
        {
            bookings = bookings.Where(b => b.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Counterparty))
        {
            var counterparty = filter.Counterparty.Trim().ToLower();
            bookings = bookings.Where(b => b.Counterparty != null && b.Counterparty.Name.ToLower() == counterparty);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            bookings = bookings.Where(b =>
                b.Description.ToLower().Contains(search)
                || (b.Note != null && b.Note.ToLower().Contains(search))
                || (b.Counterparty != null && b.Counterparty.Name.ToLower().Contains(search)));
        }

        var isClosed = await db.ClosedYears.AnyAsync(closed => closed.Year == filter.Year, cancellationToken);

        var views = await bookings
            .OrderByDescending(b => b.Date)
            .ThenByDescending(b => b.CreatedAt)
            .Select(b => new BookingView(
                b.Id,
                b.Date,
                b.Kind,
                b.AmountChf,
                b.OriginalAmount,
                b.OriginalCurrency,
                b.Description,
                b.CategoryId,
                b.Category!.Name,
                b.Counterparty != null ? b.Counterparty.Name : null,
                b.PaymentMethod != null ? b.PaymentMethod.Name : null,
                b.ReceiptNumber,
                b.Note,
                isClosed))
            .ToListAsync(cancellationToken);

        var years = await db.Bookings.Select(b => b.Date.Year).Distinct().ToListAsync(cancellationToken);
        years.Add(time.GetLocalNow().Year);
        years.Add(filter.Year);

        return Result.Success(new JournalView(
            filter.Year,
            views,
            income,
            expense,
            years.Distinct().OrderDescending().ToList(),
            isClosed));
    }
}
