using Flowtr.Server.Features.Journal.Commands;
using Flowtr.Server.Features.Journal.Model;
using Flowtr.Server.Features.Journal.Queries;
using Flowtr.Shared;
using Flowtr.Shared.Journal;
using Microsoft.EntityFrameworkCore;
using static Flowtr.Tests.Journal.JournalTestData;

namespace Flowtr.Tests.Journal;

/// <summary>
/// Story #007: enter, change and delete bookings; closed years are read-only.
/// </summary>
public class BookingTests
{
    [Fact]
    public async Task Created_booking_is_in_the_journal_with_its_names()
    {
        // Arrange
        using var app = new TestApp();

        // Act
        var id = await app.Create(Expense(amount: 20.50m) with { OriginalAmount = 22.00m, OriginalCurrency = "usd", Note = "  Oktober " });

        // Assert
        var booking = (await Journal(app, new JournalFilter(2026))).Bookings.ShouldHaveSingleItem();
        booking.ShouldBe(new BookingView(
            id, new DateOnly(2026, 10, 1), BookingKind.Expense, 20.50m, 22.00m, "USD", "Azure Abo", Software, "Software und Abos",
            "Microsoft", "Kreditkarte", null, "Oktober", IsLocked: false));
    }

    [Fact]
    public async Task New_counterparty_is_added_once_and_found_without_case()
    {
        // Arrange
        using var app = new TestApp();

        // Act
        await app.Create(Expense(counterparty: "Anthropic"));
        await app.Create(Expense(counterparty: " anthropic "));

        // Assert
        (await app.Db.Counterparties.Select(c => c.Name).ToListAsync()).ShouldBe(["Anthropic"]);
        (await app.Db.PaymentMethods.CountAsync()).ShouldBe(3);
    }

    [Theory]
    [InlineData("", 20, "Bitte eine Beschreibung angeben.")]
    [InlineData("Abo", 0, "Der Betrag in CHF muss grösser als 0 sein.")]
    [InlineData("Abo", -5, "Der Betrag in CHF muss grösser als 0 sein.")]
    [InlineData("Abo", 20.005, "Beträge haben höchstens zwei Nachkommastellen.")]
    public async Task Invalid_input_is_rejected(string description, decimal amount, string error)
    {
        // Arrange
        using var app = new TestApp();

        // Act
        var result = await app.Command<CreateBooking, Result<Guid>>(new CreateBooking(Expense(description: description, amount: amount)));

        // Assert
        result.Error.ShouldBe(error);
        (await app.Db.Bookings.CountAsync()).ShouldBe(0);
    }

    [Theory]
    [InlineData(22.0, null, "Für den Originalbetrag braucht es Betrag und Währung.")]
    [InlineData(null, "USD", "Für den Originalbetrag braucht es Betrag und Währung.")]
    [InlineData(22.0, "US", "Die Währung braucht drei Buchstaben, zum Beispiel USD.")]
    [InlineData(-1.0, "USD", "Der Originalbetrag muss grösser als 0 sein.")]
    [InlineData(60.0, "chf", "Der Originalbetrag ist nur für Fremdwährungen. Bei CHF bleibt er leer.")]
    public async Task Original_amount_needs_amount_and_currency(double? amount, string? currency, string error)
    {
        // Arrange
        using var app = new TestApp();
        var input = Expense() with { OriginalAmount = (decimal?)amount, OriginalCurrency = currency };

        // Act
        var result = await app.Command<CreateBooking, Result<Guid>>(new CreateBooking(input));

        // Assert
        result.Error.ShouldBe(error);
    }

    [Fact]
    public async Task Category_must_fit_the_kind_and_be_active()
    {
        // Arrange
        using var app = new TestApp();
        await app.Command<UpdateCategory, Result>(new UpdateCategory(Hardware, new CategoryUpdate("Hardware und Geräte", IsActive: false)));

        // Act
        var wrongKind = await app.Command<CreateBooking, Result<Guid>>(new CreateBooking(Expense(category: Revenue)));
        var inactive = await app.Command<CreateBooking, Result<Guid>>(new CreateBooking(Expense(category: Hardware)));

        // Assert
        wrongKind.Error.ShouldBe("Bitte eine passende Kategorie wählen.");
        inactive.Error.ShouldBe("Bitte eine passende Kategorie wählen.");
    }

    [Fact]
    public async Task Booking_keeps_its_category_after_deactivation()
    {
        // Arrange
        using var app = new TestApp();
        var id = await app.Create(Expense(category: Hardware));
        await app.Command<UpdateCategory, Result>(new UpdateCategory(Hardware, new CategoryUpdate("Hardware und Geräte", IsActive: false)));

        // Act
        var result = await app.Command<UpdateBooking, Result>(new UpdateBooking(id, Expense(category: Hardware, amount: 25m)));

        // Assert
        result.IsSuccess.ShouldBeTrue(result.Error);
    }

    [Fact]
    public async Task Update_and_delete_change_the_journal()
    {
        // Arrange
        using var app = new TestApp();
        var first = await app.Create(Expense(description: "Laptop", amount: 1500m, category: Hardware));
        var second = await app.Create(Expense(description: "Maus", amount: 30m, category: Hardware));

        // Act
        await app.Command<UpdateBooking, Result>(new UpdateBooking(first, Expense(description: "Laptop", amount: 1450m, category: Hardware)));
        await app.Command<DeleteBooking, Result>(new DeleteBooking(second));

        // Assert
        var journal = await Journal(app, new JournalFilter(2026));
        journal.Bookings.Select(b => (b.Description, b.AmountChf)).ShouldBe([("Laptop", 1450m)]);
    }

    [Fact]
    public async Task Journal_shows_one_year_newest_first_with_totals()
    {
        // Arrange
        using var app = new TestApp();
        await app.Create(Expense(date: "2025-12-31", amount: 999m, description: "Vorjahr"));
        await app.Create(Expense(date: "2026-01-01", amount: 20.10m, description: "Januar"));
        await app.Create(Income(date: "2026-03-15", amount: 100.25m));
        await app.Create(Expense(date: "2026-12-31", amount: 30.05m, description: "Dezember"));

        // Act
        var journal = await Journal(app, new JournalFilter(2026));

        // Assert
        journal.Bookings.Select(b => b.Description).ShouldBe(["Dezember", "App-Verkäufe", "Januar"]);
        (journal.Income, journal.Expense, journal.Profit).ShouldBe((100.25m, 50.15m, 50.10m));
        journal.Years.ShouldBe([2026, 2025]);
    }

    [Fact]
    public async Task Journal_filters_by_kind_category_counterparty_and_text()
    {
        // Arrange
        using var app = new TestApp();
        await app.Create(Expense(description: "Azure Abo", counterparty: "Microsoft"));
        await app.Create(Expense(description: "Claude Abo", counterparty: "Anthropic") with { Note = "Team-Plan" });
        await app.Create(Expense(description: "Laptop", category: Hardware, counterparty: "Digitec"));
        await app.Create(Income());

        // Act and assert
        (await Descriptions(app, new JournalFilter(2026, Kind: BookingKind.Income))).ShouldBe(["App-Verkäufe"]);
        (await Descriptions(app, new JournalFilter(2026, CategoryId: Hardware))).ShouldBe(["Laptop"]);
        (await Descriptions(app, new JournalFilter(2026, Counterparty: "anthropic"))).ShouldBe(["Claude Abo"]);
        (await Descriptions(app, new JournalFilter(2026, Search: "team"))).ShouldBe(["Claude Abo"]);
        (await Descriptions(app, new JournalFilter(2026, Search: "digi"))).ShouldBe(["Laptop"]);
        var filtered = await Journal(app, new JournalFilter(2026, Kind: BookingKind.Expense, Search: "Abo"));
        (filtered.Income, filtered.Expense).ShouldBe((100.00m, 60.00m), "the year totals do not depend on the filter");
    }

    [Fact]
    public async Task Closed_year_is_read_only()
    {
        // Arrange
        using var app = new TestApp();
        var id = await app.Create(Expense(date: "2025-06-01"));
        var open = await app.Create(Expense(date: "2026-06-01"));
        app.Db.ClosedYears.Add(new ClosedYear { Year = 2025, ClosedAt = app.Clock.Now });
        await app.Db.SaveChangesAsync();
        const string closed = "Das Jahr 2025 ist abgeschlossen. Seine Buchungen lassen sich nicht mehr ändern.";

        // Act and assert
        (await app.Command<CreateBooking, Result<Guid>>(new CreateBooking(Expense(date: "2025-07-01")))).Error.ShouldBe(closed);
        (await app.Command<UpdateBooking, Result>(new UpdateBooking(id, Expense(date: "2026-07-01")))).Error.ShouldBe(closed);
        (await app.Command<UpdateBooking, Result>(new UpdateBooking(open, Expense(date: "2025-07-01")))).Error.ShouldBe(closed);
        (await app.Command<DeleteBooking, Result>(new DeleteBooking(id))).Error.ShouldBe(closed);
        var journal = await Journal(app, new JournalFilter(2025));
        journal.IsClosed.ShouldBeTrue();
        journal.Bookings.ShouldAllBe(b => b.IsLocked);
    }

    private static async Task<JournalView> Journal(TestApp app, JournalFilter filter) =>
        (await app.Query<GetJournal, JournalView>(new GetJournal(filter))).Value!;

    private static async Task<IEnumerable<string>> Descriptions(TestApp app, JournalFilter filter) =>
        (await Journal(app, filter)).Bookings.Select(b => b.Description).ToList();
}
