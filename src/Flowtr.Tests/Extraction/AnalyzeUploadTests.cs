using Flowtr.Server.Features.Extraction;
using Flowtr.Server.Features.Extraction.Queries;
using Flowtr.Server.Features.Receipts;
using Flowtr.Shared;
using Flowtr.Shared.Journal;
using Flowtr.Tests.Receipts;
using static Flowtr.Tests.Journal.JournalTestData;

namespace Flowtr.Tests.Extraction;

/// <summary>
/// Story #009: Flowtr checks the AI answer and makes a booking proposal.
/// </summary>
public class AnalyzeUploadTests
{
    private static ReceiptReading Reading(
        string? date = "2026-10-01", string? kind = "Expense", decimal? total = 64.20m, string? currency = "CHF",
        string? description = "Azure Abo Oktober 2026", string? counterparty = "Microsoft", string? categoryId = null, string? paymentMethod = null) =>
        new(date, kind, total, currency, description, counterparty, categoryId ?? Software.ToString(), paymentMethod);

    [Fact]
    public async Task Proposal_in_chf_takes_the_values()
    {
        // Arrange
        using var app = new TestApp();
        app.Reader.Next = Result.Success(Reading(paymentMethod: "kreditkarte"));

        // Act
        var proposal = await Analyze(app);

        // Assert
        proposal.ShouldBe(new BookingProposal(
            new DateOnly(2026, 10, 1), BookingKind.Expense, 64.20m, null, null, "Azure Abo Oktober 2026", Software, "Microsoft", "Kreditkarte", null));
    }

    [Fact]
    public async Task Foreign_currency_gets_chf_by_the_daily_rate()
    {
        // Arrange
        using var app = new TestApp();
        app.Reader.Next = Result.Success(Reading(total: 55.00m, currency: "usd"));
        app.Rates.Rates["USD"] = new ExchangeRate(0.8012m, new DateOnly(2026, 9, 30), "EZB");

        // Act
        var proposal = await Analyze(app);

        // Assert
        (proposal.AmountChf, proposal.OriginalAmount, proposal.OriginalCurrency).ShouldBe((44.07m, 55.00m, "USD"));
        proposal.ExchangeRateNote.ShouldBe("Umgerechnet: USD 55.00 × 0.8012 (EZB, 30.09.2026)");
    }

    [Fact]
    public async Task Without_rate_the_chf_amount_stays_empty()
    {
        // Arrange
        using var app = new TestApp();
        app.Reader.Next = Result.Success(Reading(total: 10m, currency: "JPY"));

        // Act
        var proposal = await Analyze(app);

        // Assert
        (proposal.AmountChf, proposal.OriginalAmount, proposal.OriginalCurrency).ShouldBe((null, 10m, "JPY"));
        proposal.ExchangeRateNote.ShouldBe("Kein Kurs für JPY gefunden: bitte den Betrag in CHF selbst eintragen.");
    }

    [Fact]
    public async Task Invalid_values_are_left_out()
    {
        // Arrange
        using var app = new TestApp();
        app.Reader.Next = Result.Success(Reading(date: "gestern", kind: "Spende", total: -5m, currency: "Dollar",
            categoryId: "keine-id", paymentMethod: "Bitcoin", counterparty: new string('x', 300)));

        // Act
        var proposal = await Analyze(app);

        // Assert
        proposal.ShouldBe(new BookingProposal(null, null, null, null, null, "Azure Abo Oktober 2026", null, null, null, null));
    }

    [Fact]
    public async Task Category_must_fit_the_kind()
    {
        // Arrange
        using var app = new TestApp();
        app.Reader.Next = Result.Success(Reading(kind: "Income", categoryId: Software.ToString()));

        // Act
        var proposal = await Analyze(app);

        // Assert
        (proposal.Kind, proposal.CategoryId).ShouldBe((BookingKind.Income, null));
    }

    [Fact]
    public async Task Earlier_bookings_of_the_counterparty_decide_category_and_payment()
    {
        // Arrange
        using var app = new TestApp();
        await app.Create(Expense(date: "2026-09-01", category: Hardware, counterparty: "Microsoft") with { PaymentMethod = "Bankkonto" });
        app.Reader.Next = Result.Success(Reading(counterparty: "MICROSOFT", categoryId: Software.ToString()));

        // Act
        var proposal = await Analyze(app);

        // Assert
        (proposal.Counterparty, proposal.CategoryId, proposal.PaymentMethod).ShouldBe(("Microsoft", Hardware, "Bankkonto"));
        app.Reader.LastContext!.History.ShouldBe(["Microsoft: Hardware und Geräte, Bankkonto"]);
        app.Reader.LastContext.Categories.Count.ShouldBe(14);
    }

    [Fact]
    public async Task Reader_failure_and_missing_upload_give_a_message()
    {
        // Arrange
        using var app = new TestApp();
        app.Reader.Next = Result.Failure<ReceiptReading>(AnthropicReceiptReader.NotConfigured);

        // Act
        var failed = await app.Query<AnalyzeUpload, BookingProposal>(new AnalyzeUpload(await app.UploadPdf()));
        var missing = await app.Query<AnalyzeUpload, BookingProposal>(new AnalyzeUpload(Guid.NewGuid()));

        // Assert
        failed.Error.ShouldBe(AnthropicReceiptReader.NotConfigured);
        missing.Error.ShouldBe(ReceiptFiling.UploadGone);
    }

    private static async Task<BookingProposal> Analyze(TestApp app)
    {
        var result = await app.Query<AnalyzeUpload, BookingProposal>(new AnalyzeUpload(await app.UploadPdf()));
        result.IsSuccess.ShouldBeTrue(result.Error);
        return result.Value!;
    }
}
