using System.Text.Json;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Server.Features.Extraction.Queries;
using Flowtr.Shared;
using Flowtr.Shared.Inbox;
using Flowtr.Shared.Journal;
using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.Features.Inbox.Commands;

/// <summary>
/// Reads the new receipts in the inbox with AI (story #009). The proposal is ready when the user opens the receipt.
/// </summary>
/// <remarks>
/// The origin of the mail (sender, subject) goes to the AI as a hint for the counterparty. A failed analysis stays failed:
/// the booking dialog tries again when the user opens the receipt.
/// </remarks>
public sealed record AnalyzeInbox;

public sealed class AnalyzeInboxHandler(ApplicationDbContext db, IQueryHandler<AnalyzeUploads, BookingProposal> analyze, ILogger<AnalyzeInboxHandler> logger)
    : ICommandHandler<AnalyzeInbox, Result<int>>
{
    public async Task<Result<int>> Handle(AnalyzeInbox command, CancellationToken cancellationToken)
    {
        var pending = await db.InboxItems
            .Include(i => i.Mail)
            .Include(i => i.Files)
            .Where(i => i.Analysis == AnalysisState.Pending)
            .OrderBy(i => i.ImportedAt)
            .ToListAsync(cancellationToken);

        foreach (var item in pending)
        {
            var uploads = item.Files.OrderBy(f => f.Position).Select(f => f.UploadId).ToList();
            var result = await analyze.Handle(new AnalyzeUploads(uploads, Origin(item)), cancellationToken);
            if (result.IsSuccess)
            {
                item.Analysis = AnalysisState.Done;
                item.Proposal = JsonSerializer.Serialize(result.Value);
            }
            else
            {
                item.Analysis = AnalysisState.Failed;
                item.AnalysisError = result.Error;
                logger.LogWarning("Inbox receipt {Id} not analyzed: {Error}", item.Id, result.Error);
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(pending.Count);
    }

    /// <summary>
    /// For the AI: where the receipt comes from.
    /// </summary>
    public static string Origin(Model.InboxItem item) =>
        $"E-mail from {item.Mail!.Sender}" +
        (item.Mail.OriginalSender is { } original ? $", forwarded mail originally from {original}" : "") +
        $", subject \"{item.Mail.Subject}\", " +
        (item.Files.Count == 1 ? $"file \"{item.Files[0].FileName}\"" : $"files {string.Join(", ", item.Files.OrderBy(f => f.Position).Select(f => $"\"{f.FileName}\""))}");
}
