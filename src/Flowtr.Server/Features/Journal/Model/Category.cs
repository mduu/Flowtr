using Flowtr.Shared.Journal;

namespace Flowtr.Server.Features.Journal.Model;

/// <summary>
/// Category of a booking, close to the tax form «Selbständige Erwerbstätigkeit». Used categories are deactivated, not deleted.
/// </summary>
public sealed class Category
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Name { get; set; } = "";

    public BookingKind Kind { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}
