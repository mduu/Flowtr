using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Features.Inbox.Commands;
using Flowtr.Server.Features.Inbox.Mail;
using Flowtr.Shared;
using Flowtr.Shared.Inbox;
using Microsoft.Extensions.Options;

namespace Flowtr.Server.Features.Inbox;

/// <summary>
/// Fetches mail every <see cref="MailboxOptions.Interval"/>, analyzes new receipts and removes expired ones.
/// <see cref="MailboxState.Wake"/> starts a run at once, for example after «Jetzt abholen».
/// </summary>
public sealed class InboxWorker(IServiceScopeFactory scopes, MailboxState state, IOptions<MailboxOptions> options, ILogger<InboxWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.IsConfigured)
        {
            logger.LogInformation("No mailbox configured (section {Section}): Flowtr does not fetch mail", MailboxOptions.Section);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await Run(scope.ServiceProvider, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Inbox run failed");
            }

            await state.WaitForWake(options.Value.Interval, stoppingToken);
        }
    }

    private async Task Run(IServiceProvider services, CancellationToken cancellationToken)
    {
        if (options.Value.IsConfigured)
        {
            await services.GetRequiredService<ICommandHandler<FetchMail, Result<FetchResult>>>().Handle(new FetchMail(), cancellationToken);
        }

        await services.GetRequiredService<ICommandHandler<AnalyzeInbox, Result<int>>>().Handle(new AnalyzeInbox(), cancellationToken);
        await services.GetRequiredService<ICommandHandler<ExpireInboxItems, Result<int>>>().Handle(new ExpireInboxItems(), cancellationToken);
    }
}
