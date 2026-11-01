using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.CrossCutting.Hosting;
using Flowtr.Server.Features.Inbox.Commands;
using Flowtr.Server.Features.Inbox.Mail;
using Flowtr.Shared;
using Flowtr.Shared.Inbox;
using Microsoft.Extensions.Options;

namespace Flowtr.Server.Features.Inbox;

/// <summary>
/// While the app runs: fetches mail every <see cref="MailboxOptions.Interval"/> and at once after <see cref="MailboxState.Wake"/>
/// (for example «Jetzt abholen»).
/// </summary>
/// <remarks>
/// The database pauses when nobody uses it (story #011). Thus a run uses the database only when there is something to do:
/// the fetch only with mail in the mailbox, the analysis only for new receipts, at the start and after a wake,
/// and the removal of expired receipts once a day.
/// </remarks>
public sealed class InboxWorker(
    IServiceScopeFactory scopes,
    MailboxState state,
    DatabaseReadiness database,
    IOptions<MailboxOptions> options,
    TimeProvider time,
    ILogger<InboxWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan ExpiryInterval = TimeSpan.FromDays(1);

    private DateTimeOffset? lastExpiry;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.IsConfigured)
        {
            logger.LogInformation("No mailbox configured (section {Section}): Flowtr does not fetch mail", MailboxOptions.Section);
        }

        await database.WaitUntilReady(stoppingToken);

        var analyzeAll = true;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await Run(scope.ServiceProvider, analyzeAll, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Inbox run failed");
            }

            analyzeAll = await state.WaitForWake(options.Value.Interval, stoppingToken);
        }
    }

    /// <param name="analyzeAll">Also analyze receipts that wait from earlier, for example after a restart or «Jetzt abholen».</param>
    private async Task Run(IServiceProvider services, bool analyzeAll, CancellationToken cancellationToken)
    {
        var imported = 0;
        if (options.Value.IsConfigured)
        {
            var fetched = await services.GetRequiredService<ICommandHandler<FetchMail, Result<FetchResult>>>().Handle(new FetchMail(), cancellationToken);
            imported = fetched.Value?.Receipts ?? 0;
        }

        if (analyzeAll || imported > 0)
        {
            await services.GetRequiredService<ICommandHandler<AnalyzeInbox, Result<int>>>().Handle(new AnalyzeInbox(), cancellationToken);
        }

        if (lastExpiry is null || time.GetUtcNow() - lastExpiry >= ExpiryInterval)
        {
            await services.GetRequiredService<ICommandHandler<ExpireInboxItems, Result<int>>>().Handle(new ExpireInboxItems(), cancellationToken);
            lastExpiry = time.GetUtcNow();
        }
    }
}
