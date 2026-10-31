using Flowtr.Server.CrossCutting.Api;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Features.Inbox.Commands;
using Flowtr.Server.Features.Inbox.Mail;
using Flowtr.Server.Features.Inbox.Queries;
using Flowtr.Shared;
using Flowtr.Shared.Inbox;
using Flowtr.Shared.Users;

namespace Flowtr.Server.Features.Inbox;

public static class InboxEndpoints
{
    /// <summary>
    /// Maps <c>inbox</c> in the API group: the inbox for all roles, the allowed senders only for admins.
    /// </summary>
    public static IEndpointRouteBuilder MapInboxEndpoints(this IEndpointRouteBuilder api)
    {
        var inbox = api.MapGroup("/inbox");

        inbox.MapGet("/", async (IQueryHandler<GetInbox, InboxView> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetInbox(), cancellationToken)).ToHttp());

        inbox.MapPost("/fetch", async (ICommandHandler<FetchMail, Result<FetchResult>> handler, MailboxState state, CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new FetchMail(), cancellationToken);
            state.Wake();
            return result.ToHttp();
        });

        inbox.MapDelete("/{id:guid}", async (Guid id, ICommandHandler<DiscardInboxItem, Result> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new DiscardInboxItem(id), cancellationToken)).ToHttp());

        var senders = inbox.MapGroup("/senders").RequireAuthorization(policy => policy.RequireRole(Roles.Admin));

        senders.MapGet("/", async (IQueryHandler<GetSenderSettings, SenderSettings> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetSenderSettings(), cancellationToken)).ToHttp());

        senders.MapPost("/", async (SenderInput input, ICommandHandler<AddAllowedSender, Result<Guid>> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new AddAllowedSender(input), cancellationToken)).ToHttp());

        senders.MapDelete("/{id:guid}", async (Guid id, ICommandHandler<RemoveAllowedSender, Result> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new RemoveAllowedSender(id), cancellationToken)).ToHttp());

        return api;
    }
}
