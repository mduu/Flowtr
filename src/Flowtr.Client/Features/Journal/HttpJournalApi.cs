using System.Globalization;
using System.Net.Http.Json;
using Flowtr.Client.Api;
using Flowtr.Shared;
using Flowtr.Shared.Journal;

namespace Flowtr.Client.Features.Journal;

public sealed class HttpJournalApi(HttpClient http) : IJournalApi
{
    public Task<Result<JournalView>> GetJournal(JournalFilter filter, CancellationToken cancellationToken = default) =>
        http.GetAsync(ApiResults.Query("api/journal", new Dictionary<string, string?>
            {
                ["year"] = filter.Year.ToString(CultureInfo.InvariantCulture),
                ["kind"] = filter.Kind?.ToString(),
                ["category"] = filter.CategoryId?.ToString(),
                ["counterparty"] = filter.Counterparty,
                ["search"] = filter.Search,
            }), cancellationToken)
            .Read<JournalView>(cancellationToken);

    public Task<Result<BookingOptions>> GetOptions(CancellationToken cancellationToken = default) =>
        http.GetAsync("api/journal/options", cancellationToken).Read<BookingOptions>(cancellationToken);

    public Task<Result<Guid>> CreateBooking(BookingInput input, CancellationToken cancellationToken = default) =>
        http.PostAsJsonAsync("api/bookings", input, cancellationToken).Read<Guid>(cancellationToken);

    public Task<Result> UpdateBooking(Guid id, BookingInput input, CancellationToken cancellationToken = default) =>
        http.PutAsJsonAsync($"api/bookings/{id}", input, cancellationToken).Read(cancellationToken);

    public Task<Result> DeleteBooking(Guid id, CancellationToken cancellationToken = default) =>
        http.DeleteAsync($"api/bookings/{id}", cancellationToken).Read(cancellationToken);

    public Task<Result<IReadOnlyList<CategoryView>>> GetCategories(CancellationToken cancellationToken = default) =>
        http.GetAsync("api/categories", cancellationToken).Read<IReadOnlyList<CategoryView>>(cancellationToken);

    public Task<Result<Guid>> CreateCategory(CategoryInput input, CancellationToken cancellationToken = default) =>
        http.PostAsJsonAsync("api/categories", input, cancellationToken).Read<Guid>(cancellationToken);

    public Task<Result> UpdateCategory(Guid id, CategoryUpdate update, CancellationToken cancellationToken = default) =>
        http.PutAsJsonAsync($"api/categories/{id}", update, cancellationToken).Read(cancellationToken);

    public Task<Result> DeleteCategory(Guid id, CancellationToken cancellationToken = default) =>
        http.DeleteAsync($"api/categories/{id}", cancellationToken).Read(cancellationToken);
}
