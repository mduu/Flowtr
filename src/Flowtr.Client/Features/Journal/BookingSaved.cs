using Flowtr.Shared.Journal;

namespace Flowtr.Client.Features.Journal;

/// <summary>
/// The result of the booking dialog after a save. <see cref="MoreBookings"/> are the further bookings of the receipt
/// that the AI proposed: the journal opens them with the same <see cref="Uploads"/> (story #017).
/// </summary>
public sealed record BookingSaved(DateOnly Date, IReadOnlyList<UploadedReceipt> Uploads, IReadOnlyList<BookingProposal> MoreBookings);
