namespace Flowtr.Server.Features.Receipts;

public static class ReceiptRegistration
{
    public static IServiceCollection AddReceipts(this IServiceCollection services)
    {
        services.AddSingleton<IReceiptStore, BlobReceiptStore>();
        services.AddScoped<ReceiptFiling>();
        services.AddScoped<Flowtr.Client.Features.Receipts.IReceiptApi, ServerReceiptApi>();
        services.AddHostedService<ReceiptHousekeeping>();

        return services;
    }
}
