using Flowtr.Shared.Journal;

namespace Flowtr.Server.Features.Receipts.Model;

/// <summary>
/// The last receipt number per year and kind. <see cref="LastNumber"/> is a concurrency token:
/// two parallel saves cannot get the same number (ADR-007).
/// </summary>
public sealed class ReceiptSequence
{
    public int Year { get; set; }

    public BookingKind Kind { get; set; }

    public int LastNumber { get; set; }
}
