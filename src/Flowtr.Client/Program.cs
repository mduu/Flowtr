using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Flowtr.Client.Features.Audit;
using Flowtr.Client.Features.Journal;
using Flowtr.Client.Features.Receipts;
using Flowtr.Client.Features.Users;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();
builder.Services.AddMudServices();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IUserAdministration, HttpUserAdministration>();
builder.Services.AddScoped<IAuditLogReader, HttpAuditLogReader>();
builder.Services.AddScoped<IJournalApi, HttpJournalApi>();
builder.Services.AddScoped<IReceiptApi, HttpReceiptApi>();

await builder.Build().RunAsync();
