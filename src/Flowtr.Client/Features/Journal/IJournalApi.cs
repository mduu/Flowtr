using Flowtr.Shared;
using Flowtr.Shared.Journal;

namespace Flowtr.Client.Features.Journal;

/// <summary>
/// Journal and categories. In the browser it calls the API, on the server (prerendering) the handlers.
/// </summary>
public interface IJournalApi
{
    Task<Result<JournalView>> GetJournal(JournalFilter filter, CancellationToken cancellationToken = default);

    Task<Result<BookingOptions>> GetOptions(CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateBooking(BookingInput input, CancellationToken cancellationToken = default);

    Task<Result> UpdateBooking(Guid id, BookingInput input, CancellationToken cancellationToken = default);

    Task<Result> DeleteBooking(Guid id, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CategoryView>>> GetCategories(CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateCategory(CategoryInput input, CancellationToken cancellationToken = default);

    Task<Result> UpdateCategory(Guid id, CategoryUpdate update, CancellationToken cancellationToken = default);

    Task<Result> DeleteCategory(Guid id, CancellationToken cancellationToken = default);
}
