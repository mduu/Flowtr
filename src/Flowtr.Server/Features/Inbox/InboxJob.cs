using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.CrossCutting.Hosting;
using Flowtr.Server.Features.Inbox.Commands;
using Flowtr.Server.Features.Inbox.Mail;
using Flowtr.Shared;
using Flowtr.Shared.Inbox;
using Microsoft.Extensions.Options;

namespace Flowtr.Server.Features.Inbox;

/// <summary>
/// Fetches the mail once and lets the AI read the new receipts (story #011). In Azure, a scheduled job starts it
/// once a night, while the app sleeps. It wakes the database only when mail is in the mailbox.
/// </summary>
public sealed class InboxJob(
    IMailbox mailbox,
    IOptions<MailboxOptions> options,
    IDatabasePreparation database,
    ICommandHandler<FetchMail, Result<FetchResult>> fetch,
    ICommandHandler<AnalyzeInbox, Result<int>> analyze,
    ILogger<InboxJob> logger)
{
    /// <summary>
    /// The configuration value <c>job=inbox</c>, for example from the arguments <c>--job inbox</c>.
    /// </summary>
    public static bool IsRequested(IConfiguration configuration) => configuration["job"] == "inbox";

    /// <summary>
    /// How long the job waits for the database. Then the mail stays in the mailbox until the next run.
    /// </summary>
    public static readonly TimeSpan MaxDatabaseWait = TimeSpan.FromMinutes(3);

    /// <param name="databaseWait">How long to wait for the database, <see cref="MaxDatabaseWait"/> if null.</param>
    /// <returns>The exit code: 0 when the run succeeded, 1 when it failed. A failed run shows in Azure.</returns>
    public async Task<int> Run(CancellationToken cancellationToken, TimeSpan? databaseWait = null)
    {
        var wait = databaseWait ?? MaxDatabaseWait;
        if (!options.Value.IsConfigured)
        {
            logger.LogError("No mailbox configured (section {Section})", MailboxOptions.Section);
            return 1;
        }

        int waiting;
        try
        {
            waiting = await CountMail(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Mailbox {Host} not reachable", options.Value.Host);
            return 1;
        }

        if (waiting == 0)
        {
            return 0;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(wait);
        try
        {
            await database.Prepare(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError("Database not ready after {Wait}: {Count} mails stay in the mailbox", wait, waiting);
            return 1;
        }

        var fetched = await fetch.Handle(new FetchMail(), cancellationToken);
        if (!fetched.IsSuccess)
        {
            return 1;
        }

        if (fetched.Value!.Receipts > 0)
        {
            await analyze.Handle(new AnalyzeInbox(), cancellationToken);
        }

        return 0;
    }

    private async Task<int> CountMail(CancellationToken cancellationToken)
    {
        await using var session = await mailbox.Open(cancellationToken);
        return (await session.List(cancellationToken)).Count;
    }
}

public static class InboxJobExtensions
{
    public static async Task<int> RunInboxJob(this WebApplication app)
    {
        await using (app)
        {
            await using var scope = app.Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<InboxJob>().Run(app.Lifetime.ApplicationStopping);
        }
    }
}
