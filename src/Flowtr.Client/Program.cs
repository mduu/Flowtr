using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Flowtr.Client.Features.Audit;
using Flowtr.Client.Features.Business;
using Flowtr.Client.Features.Inbox;
using Flowtr.Client.Features.Journal;
using Flowtr.Client.Features.Receipts;
using Flowtr.Client.Features.Statistics;
using Flowtr.Client.Features.Status;
using Flowtr.Client.Features.Users;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();
builder.Services.AddMudServices();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ServerStatus>();
builder.Services.AddScoped(services => new HttpClient(new SlowCallHandler(services.GetRequiredService<ServerStatus>()) { InnerHandler = new HttpClientHandler() })
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
});
builder.Services.AddScoped<IUserAdministration, HttpUserAdministration>();
builder.Services.AddScoped<IAuditLogReader, HttpAuditLogReader>();
builder.Services.AddScoped<IJournalApi, HttpJournalApi>();
builder.Services.AddScoped<IReceiptApi, HttpReceiptApi>();
builder.Services.AddScoped<IInboxApi, HttpInboxApi>();
builder.Services.AddScoped<IBusinessApi, HttpBusinessApi>();
builder.Services.AddScoped<IStatisticsApi, HttpStatisticsApi>();

await builder.Build().RunAsync();
