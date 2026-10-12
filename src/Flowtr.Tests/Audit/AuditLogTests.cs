using System.Security.Claims;
using Flowtr.Server.CrossCutting.Audit;
using Flowtr.Server.Data;
using Flowtr.Server.Features.Audit.Queries;
using Flowtr.Server.Features.Users.Commands;
using Flowtr.Shared;
using Flowtr.Shared.Audit;
using Flowtr.Shared.Users;
using Flowtr.Tests.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Flowtr.Tests.Audit;

/// <summary>
/// Audit log: each change of an audited entity gives one entry in the same save, without secrets.
/// </summary>
public class AuditLogTests
{
    [Fact]
    public async Task Invitation_writes_user_and_role_entries_with_the_actor()
    {
        // Arrange
        using var fixture = new UsersFixture();

        // Act
        var invitation = await fixture.Invite("anna@example.ch", Roles.Admin);

        // Assert
        var entries = await fixture.Db.AuditEntries.OrderBy(entry => entry.Id).ToListAsync();
        entries.Select(entry => (entry.Action, entry.EntityType)).ShouldBe([(AuditAction.Created, "User"), (AuditAction.Created, "UserRole")]);
        entries.ShouldAllBe(entry => entry.UserId == "admin-id" && entry.UserName == "admin@example.ch" && entry.At == fixture.Clock.Now);
        entries[0].EntityId.ShouldBe(invitation.UserId);
        entries[1].EntityId.ShouldBe($"{invitation.UserId}/{ApplicationDbContext.RoleIds.Admin}");
    }

    [Fact]
    public async Task Password_shows_only_that_it_changed()
    {
        // Arrange
        using var fixture = new UsersFixture();
        var invitation = await fixture.Invite("anna@example.ch");

        // Act
        await fixture.Accept(invitation, "Sicher-123");

        // Assert
        var user = (await fixture.UserManager.FindByIdAsync(invitation.UserId))!;
        var accepted = await fixture.Db.AuditEntries.Where(entry => entry.Action == AuditAction.Modified).ToListAsync();
        accepted.ShouldNotBeEmpty();
        accepted.ShouldAllBe(entry => !entry.Changes.Contains(user.PasswordHash!) && !entry.Changes.Contains(user.SecurityStamp!));
        accepted.ShouldContain(entry => entry.Changes.Contains("\"PasswordHash\":{\"Old\":null,\"New\":\"***\"}"));
        accepted.ShouldContain(entry => entry.Changes.Contains("\"EmailConfirmed\":{\"Old\":false,\"New\":true}"));
    }

    [Fact]
    public async Task Save_without_audited_change_writes_no_entry()
    {
        // Arrange
        using var fixture = new UsersFixture();
        var invitation = await fixture.Invite("anna@example.ch");
        var before = await fixture.Db.AuditEntries.CountAsync();

        // Act (Identity changes only the concurrency stamp)
        await fixture.UserManager.UpdateAsync((await fixture.UserManager.FindByIdAsync(invitation.UserId))!);

        // Assert
        (await fixture.Db.AuditEntries.CountAsync()).ShouldBe(before);
    }

    [Fact]
    public async Task Failed_save_writes_no_entry()
    {
        // Arrange
        using var fixture = new UsersFixture();
        var invitation = await fixture.Invite("anna@example.ch");
        var before = await fixture.Db.AuditEntries.CountAsync();
        using var scope = fixture.NewScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Users.Add(new ApplicationUser { Id = invitation.UserId, UserName = "ben@example.ch", Email = "ben@example.ch" });

        // Act
        await Should.ThrowAsync<Exception>(() => db.SaveChangesAsync());

        // Assert
        (await fixture.Db.AuditEntries.CountAsync()).ShouldBe(before);
    }

    [Fact]
    public async Task Entries_cannot_be_changed_or_deleted()
    {
        // Arrange
        using var fixture = new UsersFixture();
        await fixture.Invite("anna@example.ch");
        var entry = await fixture.Db.AuditEntries.FirstAsync();

        // Act and assert
        entry.UserName = "jemand";
        Should.Throw<InvalidOperationException>(() => fixture.Db.SaveChanges());
        fixture.Db.ChangeTracker.Clear();
        fixture.Db.AuditEntries.Remove(entry);
        Should.Throw<InvalidOperationException>(() => fixture.Db.SaveChanges());
    }

    [Fact]
    public async Task Query_shows_newest_first_in_german()
    {
        // Arrange
        using var fixture = new UsersFixture();
        var invitation = await fixture.Invite("anna@example.ch", Roles.Contributor);
        fixture.Clock.Now = fixture.Clock.Now.AddMinutes(5);
        fixture.Actor.Current = (null, AuditActor.Anonymous);
        await fixture.Accept(invitation);

        // Act
        var result = await fixture.Query<GetAuditEntries, AuditPage>(new GetAuditEntries(new AuditFilter(null, null, null, null)));

        // Assert
        var page = result.Value!;
        page.Total.ShouldBe(3);
        var newest = page.Entries[0];
        (newest.UserName, newest.Action, newest.Area, newest.Subject).ShouldBe(("Anonym", "Geändert", "Benutzer", "anna@example.ch"));
        newest.Changes.ShouldContain(new AuditChangeView("E-Mail bestätigt", "Nein", "Ja"));
        newest.Changes.ShouldContain(new AuditChangeView("Passwort", null, "verborgen"));
        page.Entries.Last().Subject.ShouldBe("anna@example.ch");
        page.Entries.ShouldContain(entry => entry.Area == "Rolle" && entry.Subject == "anna@example.ch: Contributor");
    }

    [Fact]
    public async Task Query_filters_by_area_user_and_time()
    {
        // Arrange
        using var fixture = new UsersFixture();
        await fixture.Invite("anna@example.ch");
        fixture.Clock.Now = fixture.Clock.Now.AddDays(1);
        fixture.Actor.Current = ("other", "ben@example.ch");
        await fixture.Invite("carla@example.ch");

        // Act
        var roles = await Read(fixture, new AuditFilter(null, null, null, "UserRole"));
        var ben = await Read(fixture, new AuditFilter(null, null, "ben@", null));
        var firstDay = await Read(fixture, new AuditFilter(null, fixture.Clock.Now.AddHours(-1), null, null));

        // Assert
        roles.Total.ShouldBe(2);
        ben.Total.ShouldBe(2);
        firstDay.Entries.ShouldAllBe(entry => entry.UserName == "admin@example.ch");
    }

    [Theory]
    [InlineData(false, false, AuditActor.System)]
    [InlineData(true, false, AuditActor.Anonymous)]
    [InlineData(true, true, "anna@example.ch")]
    public void Actor_is_user_anonymous_or_system(bool request, bool signedIn, string expected)
    {
        // Arrange
        var accessor = new HttpContextAccessor();
        if (request)
        {
            accessor.HttpContext = new DefaultHttpContext();
            if (signedIn)
            {
                accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "anna-id"), new Claim(ClaimTypes.Name, "anna@example.ch")], "Test"));
            }
        }

        // Act
        var (_, name) = new AuditActor(accessor).Current;

        // Assert
        name.ShouldBe(expected);
    }

    [Fact]
    public async Task Query_shows_email_of_deleted_user()
    {
        // Arrange
        using var fixture = new UsersFixture();
        var invitation = await fixture.Invite("anna@example.ch", Roles.Contributor);
        await fixture.Command<RevokeInvitation, Result>(new RevokeInvitation(invitation.UserId));

        // Act
        var page = await Read(fixture, new AuditFilter(null, null, null, null));

        // Assert
        page.Entries.ShouldContain(entry => entry.Action == "Gelöscht" && entry.Subject == "anna@example.ch");
        page.Entries.ShouldAllBe(entry => entry.Subject.StartsWith("anna@example.ch"));
    }

    private static async Task<AuditPage> Read(UsersFixture fixture, AuditFilter filter) =>
        (await fixture.Query<GetAuditEntries, AuditPage>(new GetAuditEntries(filter))).Value!;
}
