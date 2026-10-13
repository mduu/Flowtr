using Flowtr.Client.Features.Journal;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Features.Journal.Commands;
using Flowtr.Server.Features.Journal.Queries;
using Flowtr.Shared;
using Flowtr.Shared.Journal;
using Flowtr.Shared.Users;

namespace Flowtr.Server.Features.Journal;

/// <summary>
/// <see cref="IJournalApi"/> for prerendering on the server. It calls the handlers directly, with the same role checks as the API.
/// </summary>
public sealed class ServerJournalApi(IServiceProvider services, IHttpContextAccessor httpContextAccessor) : IJournalApi
{
    private const string NotAllowed = "Dafür hast du keine Berechtigung.";

    public Task<Result<JournalView>> GetJournal(JournalFilter filter, CancellationToken cancellationToken = default) =>
        Member(() => Query<GetJournal, JournalView>(new GetJournal(filter), cancellationToken));

    public Task<Result<BookingOptions>> GetOptions(CancellationToken cancellationToken = default) =>
        Member(() => Query<GetBookingOptions, BookingOptions>(new GetBookingOptions(), cancellationToken));

    public Task<Result<Guid>> CreateBooking(BookingInput input, CancellationToken cancellationToken = default) =>
        Member(() => Command<CreateBooking, Result<Guid>>(new CreateBooking(input), cancellationToken));

    public Task<Result> UpdateBooking(Guid id, BookingInput input, CancellationToken cancellationToken = default) =>
        Member(() => Command<UpdateBooking, Result>(new UpdateBooking(id, input), cancellationToken));

    public Task<Result> DeleteBooking(Guid id, CancellationToken cancellationToken = default) =>
        Member(() => Command<DeleteBooking, Result>(new DeleteBooking(id), cancellationToken));

    public Task<Result<IReadOnlyList<CategoryView>>> GetCategories(CancellationToken cancellationToken = default) =>
        Admin(() => Query<GetCategories, IReadOnlyList<CategoryView>>(new GetCategories(), cancellationToken));

    public Task<Result<Guid>> CreateCategory(CategoryInput input, CancellationToken cancellationToken = default) =>
        Admin(() => Command<CreateCategory, Result<Guid>>(new CreateCategory(input), cancellationToken));

    public Task<Result> UpdateCategory(Guid id, CategoryUpdate update, CancellationToken cancellationToken = default) =>
        Admin(() => Command<UpdateCategory, Result>(new UpdateCategory(id, update), cancellationToken));

    public Task<Result> DeleteCategory(Guid id, CancellationToken cancellationToken = default) =>
        Admin(() => Command<DeleteCategory, Result>(new DeleteCategory(id), cancellationToken));

    private Task<Result<T>> Member<T>(Func<Task<Result<T>>> call) =>
        IsMember ? call() : Task.FromResult(Result.Failure<T>(NotAllowed));

    private Task<Result> Member(Func<Task<Result>> call) =>
        IsMember ? call() : Task.FromResult(Result.Failure(NotAllowed));

    private Task<Result<T>> Admin<T>(Func<Task<Result<T>>> call) =>
        IsAdmin ? call() : Task.FromResult(Result.Failure<T>(NotAllowed));

    private Task<Result> Admin(Func<Task<Result>> call) =>
        IsAdmin ? call() : Task.FromResult(Result.Failure(NotAllowed));

    private bool IsMember => Roles.All.Any(role => httpContextAccessor.HttpContext?.User.IsInRole(role) == true);

    private bool IsAdmin => httpContextAccessor.HttpContext?.User.IsInRole(Roles.Admin) == true;

    private Task<Result<TResult>> Query<TQuery, TResult>(TQuery query, CancellationToken cancellationToken) =>
        services.GetRequiredService<IQueryHandler<TQuery, TResult>>().Handle(query, cancellationToken);

    private Task<TResult> Command<TCommand, TResult>(TCommand command, CancellationToken cancellationToken) where TResult : Result =>
        services.GetRequiredService<ICommandHandler<TCommand, TResult>>().Handle(command, cancellationToken);
}
