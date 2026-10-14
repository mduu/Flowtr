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
using Flowtr.Server.Data;
using Flowtr.Server.Features.Appearance;
using Flowtr.Server.Features.Audit;
using Flowtr.Server.Features.Extraction;
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
    .UseSqlServer(builder.Configuration.GetConnectionString("flowtr"))
    .AddInterceptors(services.GetRequiredService<AuditInterceptor>()));
builder.EnrichSqlServerDbContext<ApplicationDbContext>();
builder.AddAzureBlobServiceClient("blobs");
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddUsers();
builder.Services.AddScoped<IAuditLogReader, ServerAuditLogReader>();
builder.Services.AddJournal();
builder.Services.AddReceipts();
builder.Services.AddExtraction(builder.Configuration);

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddMudServices();
builder.Services.AddHandlers(typeof(Program).Assembly);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();

    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // HSTS max-age is 30 days by default. Production may need a longer value, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

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
