using Flowtr.Server.CrossCutting.Api;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Features.Journal.Commands;
using Flowtr.Server.Features.Journal.Queries;
using Flowtr.Shared;
using Flowtr.Shared.Journal;
using Flowtr.Shared.Users;
using Microsoft.AspNetCore.Mvc;

namespace Flowtr.Server.Features.Journal;

public static class JournalEndpoints
{
    /// <summary>
    /// Maps the journal API in the API group: bookings for all roles, category management only for admins.
    /// </summary>
    public static IEndpointRouteBuilder MapJournalEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGet("/journal", async ([AsParameters] JournalQuery query, IQueryHandler<GetJournal, JournalView> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetJournal(query.ToFilter()), cancellationToken)).ToHttp());

        api.MapGet("/journal/options", async (IQueryHandler<GetBookingOptions, BookingOptions> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetBookingOptions(), cancellationToken)).ToHttp());

        var bookings = api.MapGroup("/bookings");

        bookings.MapPost("/", async (BookingInput input, ICommandHandler<CreateBooking, Result<Guid>> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new CreateBooking(input), cancellationToken)).ToHttp());

        bookings.MapPut("/{id:guid}", async (Guid id, BookingInput input, ICommandHandler<UpdateBooking, Result> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new UpdateBooking(id, input), cancellationToken)).ToHttp());

        bookings.MapDelete("/{id:guid}", async (Guid id, ICommandHandler<DeleteBooking, Result> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new DeleteBooking(id), cancellationToken)).ToHttp());

        var categories = api.MapGroup("/categories").RequireAuthorization(policy => policy.RequireRole(Roles.Admin));

        categories.MapGet("/", async (IQueryHandler<GetCategories, IReadOnlyList<CategoryView>> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetCategories(), cancellationToken)).ToHttp());

        categories.MapPost("/", async (CategoryInput input, ICommandHandler<CreateCategory, Result<Guid>> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new CreateCategory(input), cancellationToken)).ToHttp());

        categories.MapPut("/{id:guid}", async (Guid id, CategoryUpdate update, ICommandHandler<UpdateCategory, Result> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new UpdateCategory(id, update), cancellationToken)).ToHttp());

        categories.MapDelete("/{id:guid}", async (Guid id, ICommandHandler<DeleteCategory, Result> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new DeleteCategory(id), cancellationToken)).ToHttp());

        return api;
    }

    private sealed record JournalQuery(
        [FromQuery] int Year,
        [FromQuery] BookingKind? Kind,
        [FromQuery] Guid? Category,
        [FromQuery] string? Counterparty,
        [FromQuery] string? Search)
    {
        public JournalFilter ToFilter() => new(Year, Kind, Category, Counterparty, Search);
    }
}
