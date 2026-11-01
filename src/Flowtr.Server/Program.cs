using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Flowtr.Client.Features.Audit;
using Flowtr.Server.Components;
using Flowtr.Server.Components.Account;
using Flowtr.Server.CrossCutting.Audit;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.CrossCutting.Hosting;
using Flowtr.Server.Data;
using Flowtr.Server.Features.Appearance;
using Flowtr.Server.Features.Audit;
using Flowtr.Server.Features.Extraction;
using Flowtr.Server.Features.Inbox;
using Flowtr.Server.Features.Journal;
using Flowtr.Server.Features.Receipts;
using Flowtr.Server.Features.Users;
using Flowtr.Shared.Users;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(options =>
{
    // The API answers with a status code, not with a redirect to the login page.
    options.Events.OnRedirectToLogin = context => RespondOrRedirect(context, StatusCodes.Status401Unauthorized);
    options.Events.OnRedirectToAccessDenied = context => RespondOrRedirect(context, StatusCodes.Status403Forbidden);
});
builder.Services.AddAuthorization();

builder.Services.AddAudit();
builder.Services.AddDbContext<ApplicationDbContext>((services, options) => options
    // Azure SQL (free tier) pauses and needs up to a minute to resume: the retries wait about 75 seconds (story #011).
    .UseSqlServer(builder.Configuration.GetConnectionString("flowtr"), sql => sql.EnableRetryOnFailure(8, TimeSpan.FromSeconds(15), null))
    .AddInterceptors(services.GetRequiredService<AuditInterceptor>(), services.GetRequiredService<DatabaseWakeInterceptor>()));
builder.EnrichSqlServerDbContext<ApplicationDbContext>();
builder.AddAzureBlobServiceClient("blobs");
builder.Services.AddAppCulture(builder.Configuration);
builder.Services.AddDatabaseStartup();
builder.Services.AddKeyStorage(builder.Configuration);
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddUsers();
builder.Services.AddScoped<IAuditLogReader, ServerAuditLogReader>();
builder.Services.AddJournal();
builder.Services.AddReceipts();
builder.Services.AddExtraction(builder.Configuration);
builder.Services.AddInbox(builder.Configuration);

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddMudServices();
builder.Services.AddSingleton<Flowtr.Client.Features.Status.ServerStatus>();
builder.Services.AddHandlers(typeof(Program).Assembly);

var app = builder.Build();

// The same image runs as the inbox job in Azure: fetch mail once, then stop (story #011).
if (InboxJob.IsRequested(app.Configuration))
{
    Environment.ExitCode = await app.RunInboxJob();
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAppCulture();
app.UseSecurityHeadersAndStartupGate();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Flowtr.Client._Imports).Assembly);

app.MapAdditionalIdentityEndpoints();
app.MapAppearanceEndpoints();

// Without a role, a user gets no data (ADR-004).
var api = app.MapGroup("/api").RequireAuthorization(policy => policy.RequireRole(Roles.All));
api.MapUsersEndpoints();
api.MapAuditEndpoints();
api.MapJournalEndpoints();
api.MapReceiptEndpoints();
api.MapInboxEndpoints();

app.MapDefaultEndpoints();

app.SetUpConfiguredAdminsOnStart();

app.Run();

static Task RespondOrRedirect(RedirectContext<CookieAuthenticationOptions> context, int statusCode)
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = statusCode;
    }
    else
    {
        context.Response.Redirect(context.RedirectUri);
    }

    return Task.CompletedTask;
}
