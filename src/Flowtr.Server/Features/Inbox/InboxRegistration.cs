using Flowtr.Client.Features.Inbox;
using Flowtr.Server.Features.Inbox.Mail;

namespace Flowtr.Server.Features.Inbox;

public static class InboxRegistration
{
    public static IServiceCollection AddInbox(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MailboxOptions>(configuration.GetSection(MailboxOptions.Section));
        services.AddSingleton<MailboxState>();
        services.AddSingleton<IMailbox, ImapMailbox>();
        services.AddScoped<SenderCheck>();
        services.AddScoped<IInboxApi, ServerInboxApi>();
        services.AddScoped<InboxJob>();
        services.AddHostedService<InboxWorker>();

        return services;
    }
}
