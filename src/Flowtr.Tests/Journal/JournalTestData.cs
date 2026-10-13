using Flowtr.Server.Features.Journal.Commands;
using Flowtr.Shared;
using Flowtr.Shared.Journal;

namespace Flowtr.Tests.Journal;

internal static class JournalTestData
{
    public static readonly Guid Software = Guid.Parse("0199d6a0-0002-7000-8000-000000000003");

    public static readonly Guid Hardware = Guid.Parse("0199d6a0-0002-7000-8000-000000000004");

    public static readonly Guid Revenue = Guid.Parse("0199d6a0-0001-7000-8000-000000000001");

    public static BookingInput Expense(
        string date = "2026-10-01",
        decimal amount = 20.00m,
        string description = "Azure Abo",
        Guid? category = null,
        string? counterparty = "Microsoft") =>
        new(DateOnly.Parse(date), BookingKind.Expense, amount, null, null, description, category ?? Software, counterparty, "Kreditkarte", null);

    public static BookingInput Income(string date = "2026-10-02", decimal amount = 100.00m, string description = "App-Verkäufe") =>
        new(DateOnly.Parse(date), BookingKind.Income, amount, null, null, description, Revenue, "Stripe", "Bankkonto", null);

    public static async Task<Guid> Create(this TestApp app, BookingInput input)
    {
        var result = await app.Command<CreateBooking, Result<Guid>>(new CreateBooking(input));
        result.IsSuccess.ShouldBeTrue(result.Error);
        return result.Value;
    }
}
