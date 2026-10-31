using Flowtr.Server.Data;
using Flowtr.Server.Features.Inbox.Mail;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using MimeKit.Cryptography;

namespace Flowtr.Server.Features.Inbox;

/// <summary>
/// Which mails Flowtr takes into the inbox (ADR-009): only from users with a role and from allowed senders,
/// and not when the provider found the sender forged (SPF, DKIM, DMARC).
/// </summary>
public sealed class SenderCheck(ApplicationDbContext db)
{
    public const string NotAllowed = "Absender nicht erlaubt";

    public const string NotConfirmed = "Absender nicht bestätigt (SPF, DKIM, DMARC)";

    /// <returns>Why Flowtr rejects the mail, or null.</returns>
    public async Task<string?> Reject(MimeMessage message, CancellationToken cancellationToken)
    {
        if (MailReceipts.Sender(message) is not { } sender || !await IsAllowed(sender, cancellationToken))
        {
            return NotAllowed;
        }

        return IsForged(message) ? NotConfirmed : null;
    }

    /// <summary>
    /// The addresses of the users with a role, lower case.
    /// </summary>
    public async Task<IReadOnlyList<string>> UserAddresses(CancellationToken cancellationToken) =>
        await db.Users
            .Where(user => user.Email != null && db.UserRoles.Any(role => role.UserId == user.Id))
            .Select(user => user.Email!.ToLower())
            .OrderBy(email => email)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Reads the topmost <c>Authentication-Results</c> header: the provider sets it on receipt. Without it, nothing is forged.
    /// </summary>
    public static bool IsForged(MimeMessage message)
    {
        var header = message.Headers.FirstOrDefault(h => h.Id == HeaderId.AuthenticationResults);
        if (header is null || !AuthenticationResults.TryParse(header.RawValue, out var results))
        {
            return false;
        }

        string? Result(string method) =>
            results.Results.FirstOrDefault(r => string.Equals(r.Method, method, StringComparison.OrdinalIgnoreCase))?.Result?.ToLowerInvariant();

        return Result("dmarc") == "fail" || (Result("spf") is "fail" or "softfail" && Result("dkim") != "pass");
    }

    private async Task<bool> IsAllowed(string sender, CancellationToken cancellationToken)
    {
        var domain = sender[sender.LastIndexOf('@')..];
        return await db.AllowedSenders.AnyAsync(s => s.Address == sender || s.Address == domain, cancellationToken)
               || (await UserAddresses(cancellationToken)).Contains(sender);
    }
}
