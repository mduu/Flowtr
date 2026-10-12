using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Server.Features.Users;
using Flowtr.Server.Features.Users.Commands;
using Flowtr.Shared;
using Flowtr.Shared.Users;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flowtr.Tests.Users;

/// <summary>
/// The user feature with Identity on an in-memory database.
/// </summary>
internal sealed class UsersFixture : IDisposable
{
    public static readonly Uri BaseUri = new("https://flowtr.test/");

    private readonly ServiceProvider provider;
    private readonly IServiceScope scope;

    public UsersFixture(string[]? configuredAdmins = null, Action<IServiceCollection>? configure = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection((configuredAdmins ?? []).Select((email, index) =>
                new KeyValuePair<string, string?>($"{ConfiguredAdmins.Section}:{index}", email)))
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<TimeProvider>(Clock);
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddUsers();
        services.AddHandlers(typeof(Program).Assembly);
        configure?.Invoke(services);

        provider = services.BuildServiceProvider();
        scope = provider.CreateScope();
        Get<ApplicationDbContext>().Database.EnsureCreated();
    }

    public TestClock Clock { get; } = new();

    public UserManager<ApplicationUser> UserManager => Get<UserManager<ApplicationUser>>();

    public T Get<T>() where T : notnull => scope.ServiceProvider.GetRequiredService<T>();

    public Task<TResult> Command<TCommand, TResult>(TCommand command) where TResult : Result =>
        Get<ICommandHandler<TCommand, TResult>>().Handle(command, CancellationToken.None);

    public Task<Result<TResult>> Query<TQuery, TResult>(TQuery query) =>
        Get<IQueryHandler<TQuery, TResult>>().Handle(query, CancellationToken.None);

    public async Task<Invitation> Invite(string email, string role = Roles.Contributor)
    {
        var result = await Command<InviteUser, Result<Invitation>>(new InviteUser(email, role, BaseUri));
        result.IsSuccess.ShouldBeTrue(result.Error);
        return result.Value!;
    }

    public Task<Result> Accept(Invitation invitation, string password = "Sicher-123") =>
        Command<AcceptInvitation, Result>(new AcceptInvitation(invitation.UserId, Code(invitation), password));

    public static string Code(Invitation invitation) =>
        Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(invitation.Link).Query)["code"].ToString();

    public void Dispose()
    {
        scope.Dispose();
        provider.Dispose();
    }
}

internal sealed class TestClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 10, 12, 8, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Now;
}
