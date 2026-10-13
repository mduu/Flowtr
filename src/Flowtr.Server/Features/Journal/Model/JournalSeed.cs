using Flowtr.Shared.Journal;

namespace Flowtr.Server.Features.Journal.Model;

/// <summary>
/// Start lists. Fixed IDs, because the migration inserts them. Admins can add and deactivate categories later.
/// </summary>
internal static class JournalSeed
{
    public static readonly Category[] Categories =
    [
        Category("0199d6a0-0001-7000-8000-000000000001", BookingKind.Income, 10, "Umsatz und Honorare"),
        Category("0199d6a0-0001-7000-8000-000000000002", BookingKind.Income, 20, "Übrige Erträge"),
        Category("0199d6a0-0002-7000-8000-000000000001", BookingKind.Expense, 10, "Material und Waren"),
        Category("0199d6a0-0002-7000-8000-000000000002", BookingKind.Expense, 20, "Fremdleistungen"),
        Category("0199d6a0-0002-7000-8000-000000000003", BookingKind.Expense, 30, "Software und Abos"),
        Category("0199d6a0-0002-7000-8000-000000000004", BookingKind.Expense, 40, "Hardware und Geräte"),
        Category("0199d6a0-0002-7000-8000-000000000005", BookingKind.Expense, 50, "Raumkosten"),
        Category("0199d6a0-0002-7000-8000-000000000006", BookingKind.Expense, 60, "Fahrzeug und Reisen"),
        Category("0199d6a0-0002-7000-8000-000000000007", BookingKind.Expense, 70, "Telefon und Internet"),
        Category("0199d6a0-0002-7000-8000-000000000008", BookingKind.Expense, 80, "Werbung und Marketing"),
        Category("0199d6a0-0002-7000-8000-000000000009", BookingKind.Expense, 90, "Weiterbildung und Fachliteratur"),
        Category("0199d6a0-0002-7000-8000-00000000000a", BookingKind.Expense, 100, "Versicherungen und Gebühren"),
        Category("0199d6a0-0002-7000-8000-00000000000b", BookingKind.Expense, 110, "Bank- und Kreditkartenspesen"),
        Category("0199d6a0-0002-7000-8000-00000000000c", BookingKind.Expense, 120, "Übriger Aufwand"),
    ];

    public static readonly PaymentMethod[] PaymentMethods =
    [
        new() { Id = Guid.Parse("0199d6a0-0003-7000-8000-000000000001"), Name = "Bankkonto" },
        new() { Id = Guid.Parse("0199d6a0-0003-7000-8000-000000000002"), Name = "Kreditkarte" },
        new() { Id = Guid.Parse("0199d6a0-0003-7000-8000-000000000003"), Name = "Bar" },
    ];

    private static Category Category(string id, BookingKind kind, int sortOrder, string name) =>
        new() { Id = Guid.Parse(id), Kind = kind, SortOrder = sortOrder, Name = name };
}
