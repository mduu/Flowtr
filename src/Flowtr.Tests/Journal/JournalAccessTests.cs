using System.Security.Claims;
using Flowtr.Client.Features.Journal;
using Flowtr.Server.Features.Audit.Queries;
using Flowtr.Shared.Audit;
using Flowtr.Shared.Journal;
using Flowtr.Shared.Users;
using Microsoft.AspNetCore.Http;
using static Flowtr.Tests.Journal.JournalTestData;

namespace Flowtr.Tests.Journal;

/// <summary>
/// Without a role no journal data, categories only for admins. Each change is in the audit log.
/// </summary>
public class JournalAccessTests
{
    [Theory]
    [InlineData(null, false, false)]
    [InlineData(Roles.Contributor, true, false)]
    [InlineData(Roles.Admin, true, true)]
    public async Task Roles_decide_about_journal_and_categories(string? role, bool journal, bool categories)
    {
        // Arrange
        using var app = new TestApp();
        app.Get<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                role is null ? [new Claim(ClaimTypes.Name, "anna@example.ch")] : [new Claim(ClaimTypes.Name, "anna@example.ch"), new Claim(ClaimTypes.Role, role)],
                "Test")),
        };
        var api = app.Get<IJournalApi>();

        // Act
        var journalResult = await api.GetJournal(new JournalFilter(2026));
        var categoriesResult = await api.GetCategories();

        // Assert
        journalResult.IsSuccess.ShouldBe(journal);
        categoriesResult.IsSuccess.ShouldBe(categories);
    }

    [Fact]
    public async Task Booking_changes_are_in_the_audit_log()
    {
        // Arrange
        using var app = new TestApp();

        // Act
        await app.Create(Expense(date: "2026-10-01", amount: 1234.5m, description: "Laptop", category: Hardware, counterparty: "Digitec"));

        // Assert
        var page = (await app.Query<GetAuditEntries, AuditPage>(new GetAuditEntries(new AuditFilter(null, null, null, "Booking")))).Value!;
        var entry = page.Entries.ShouldHaveSingleItem();
        (entry.Action, entry.Area, entry.Subject).ShouldBe(("Erstellt", "Buchung", "01.10.2026 · Laptop"));
        entry.Changes.ShouldContain(new AuditChangeView("Betrag CHF", null, "1'234.50"));
        entry.Changes.ShouldContain(new AuditChangeView("Kategorie", null, "Hardware und Geräte"));
        entry.Changes.ShouldContain(new AuditChangeView("Gegenpartei", null, "Digitec"));
        entry.Changes.ShouldContain(new AuditChangeView("Art", null, "Ausgabe"));
        entry.Changes.ShouldContain(new AuditChangeView("Datum", null, "01.10.2026"));
    }
}
