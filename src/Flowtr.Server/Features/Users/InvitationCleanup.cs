using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Features.Users.Commands;
using Flowtr.Shared;

namespace Flowtr.Server.Features.Users;

/// <summary>
/// Deletes expired invitations at start and then once a day.
/// </summary>
public sealed class InvitationCleanup(IServiceScopeFactory scopes, ILogger<InvitationCleanup> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));

        do
        {
            await DeleteExpiredInvitations(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task DeleteExpiredInvitations(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<DeleteExpiredInvitations, Result<int>>>();
            await handler.Handle(new DeleteExpiredInvitations(), stoppingToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Expired invitations not deleted");
        }
    }
}
