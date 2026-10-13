using Flowtr.Client.Features.Journal;

namespace Flowtr.Server.Features.Journal;

public static class JournalRegistration
{
    public static IServiceCollection AddJournal(this IServiceCollection services)
    {
        services.AddScoped<BookingRules>();
        services.AddScoped<IJournalApi, ServerJournalApi>();

        return services;
    }
}
