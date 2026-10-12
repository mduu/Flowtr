using Flowtr.Server.Data;
using Flowtr.Server.Features.Users;
using Flowtr.Server.Features.Users.Commands;
using Flowtr.Server.Features.Users.Queries;
using Flowtr.Shared;
using Flowtr.Shared.Users;

namespace Flowtr.Tests.Users;

/// <summary>
/// Admins manage roles in the user list. Configured admins always get the Admin role, but Flowtr never removes it automatically.
/// </summary>
public class RoleTests
{
    [Fact]
    public async Task Change_roles_adds_and_removes()
    {
        // Arrange
        using var fixture = new UsersFixture();
        var invitation = await fixture.Invite("anna@example.ch", Roles.Contributor);

        // Act
        var result = await fixture.Command<ChangeRoles, Result>(new ChangeRoles(invitation.UserId, [Roles.Admin]));

        // Assert
        result.IsSuccess.ShouldBeTrue(result.Error);
        (await fixture.UserManager.GetRolesAsync((await fixture.UserManager.FindByIdAsync(invitation.UserId))!)).ShouldBe([Roles.Admin]);
    }

    [Fact]
    public async Task Last_admin_keeps_the_admin_role()
    {
        // Arrange
        using var fixture = new UsersFixture();
        var admin = await fixture.Invite("anna@example.ch", Roles.Admin);

        // Act
        var result = await fixture.Command<ChangeRoles, Result>(new ChangeRoles(admin.UserId, [Roles.Contributor]));

        // Assert
        result.Error.ShouldBe("Flowtr braucht mindestens einen Admin. Gib zuerst einem anderen Benutzer die Rolle Admin.");
    }

    [Fact]
    public async Task Admin_role_can_go_when_another_admin_exists()
    {
        // Arrange
        using var fixture = new UsersFixture();
        var anna = await fixture.Invite("anna@example.ch", Roles.Admin);
        await fixture.Invite("ben@example.ch", Roles.Admin);

        // Act
        var result = await fixture.Command<ChangeRoles, Result>(new ChangeRoles(anna.UserId, []));

        // Assert
        result.IsSuccess.ShouldBeTrue(result.Error);
    }

    [Fact]
    public async Task Change_roles_rejects_unknown_role()
    {
        // Arrange
        using var fixture = new UsersFixture();
        var invitation = await fixture.Invite("anna@example.ch");

        // Act
        var result = await fixture.Command<ChangeRoles, Result>(new ChangeRoles(invitation.UserId, ["Chef"]));

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task User_list_shows_roles_and_open_invitations()
    {
        // Arrange
        using var fixture = new UsersFixture();
        var anna = await fixture.Invite("anna@example.ch", Roles.Admin);
        await fixture.Accept(anna);
        await fixture.Invite("ben@example.ch", Roles.Contributor);

        // Act
        var result = await fixture.Query<GetUsers, IReadOnlyList<UserSummary>>(new GetUsers());

        // Assert
        result.Value!.Select(user => (user.Email, string.Join(",", user.Roles), user.InvitationOpen))
            .ShouldBe([("anna@example.ch", Roles.Admin, false), ("ben@example.ch", Roles.Contributor, true)]);
    }

    [Fact]
    public async Task Configured_admin_gets_admin_role_when_address_is_confirmed()
    {
        // Arrange
        using var fixture = new UsersFixture(configuredAdmins: ["Anna@Example.ch"]);
        var invitation = await fixture.Invite("anna@example.ch", Roles.Contributor);
        var admins = fixture.Get<ConfiguredAdmins>();
        var user = (await fixture.UserManager.FindByIdAsync(invitation.UserId))!;

        // Act
        await admins.Grant(user);
        var beforeAccept = await fixture.UserManager.IsInRoleAsync(user, Roles.Admin);
        await fixture.Accept(invitation);
        user = (await fixture.UserManager.FindByIdAsync(invitation.UserId))!;
        await admins.Grant(user);

        // Assert
        beforeAccept.ShouldBeFalse();
        (await fixture.UserManager.IsInRoleAsync(user, Roles.Admin)).ShouldBeTrue();
    }

    [Fact]
    public async Task Admin_role_stays_when_address_is_no_longer_configured()
    {
        // Arrange
        using var fixture = new UsersFixture();
        var invitation = await fixture.Invite("anna@example.ch", Roles.Admin);
        await fixture.Accept(invitation);
        var user = (await fixture.UserManager.FindByIdAsync(invitation.UserId))!;

        // Act
        await fixture.Get<ConfiguredAdmins>().Grant(user);

        // Assert
        (await fixture.UserManager.IsInRoleAsync(user, Roles.Admin)).ShouldBeTrue();
    }

    [Fact]
    public async Task Set_up_invites_configured_admin_without_account()
    {
        // Arrange
        using var fixture = new UsersFixture(configuredAdmins: ["anna@example.ch"]);

        // Act
        var result = await fixture.Command<SetUpConfiguredAdmins, Result>(new SetUpConfiguredAdmins(UsersFixture.BaseUri));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        ApplicationUser user = (await fixture.UserManager.FindByEmailAsync("anna@example.ch"))!;
        user.EmailConfirmed.ShouldBeFalse();
        (await fixture.UserManager.GetRolesAsync(user)).ShouldBe([Roles.Admin]);
    }

    [Fact]
    public async Task Restart_keeps_earlier_admin_invitation_link_valid()
    {
        // Arrange
        using var fixture = new UsersFixture(configuredAdmins: ["anna@example.ch"]);
        var invitedAt = fixture.Clock.Now;
        await fixture.Command<SetUpConfiguredAdmins, Result>(new SetUpConfiguredAdmins(UsersFixture.BaseUri));
        var user = (await fixture.UserManager.FindByEmailAsync("anna@example.ch"))!;
        var earlierLink = await fixture.Get<Invitations>().CreateLink(user, UsersFixture.BaseUri);
        fixture.Clock.Now = fixture.Clock.Now.AddDays(3);

        // Act
        await fixture.Command<SetUpConfiguredAdmins, Result>(new SetUpConfiguredAdmins(UsersFixture.BaseUri));

        // Assert
        var check = await fixture.Query<CheckInvitation, string>(new CheckInvitation(earlierLink.UserId, UsersFixture.Code(earlierLink)));
        check.IsSuccess.ShouldBeTrue(check.Error);
        (await fixture.UserManager.FindByEmailAsync("anna@example.ch"))!.InvitedAt.ShouldBe(invitedAt);
    }

    [Fact]
    public async Task Restart_grants_admin_role_to_confirmed_configured_admin()
    {
        // Arrange
        using var fixture = new UsersFixture(configuredAdmins: ["anna@example.ch"]);
        var invitation = await fixture.Invite("anna@example.ch", Roles.Contributor);
        await fixture.Accept(invitation);

        // Act
        await fixture.Command<SetUpConfiguredAdmins, Result>(new SetUpConfiguredAdmins(UsersFixture.BaseUri));

        // Assert
        var roles = await fixture.UserManager.GetRolesAsync((await fixture.UserManager.FindByIdAsync(invitation.UserId))!);
        roles.ShouldBe([Roles.Admin, Roles.Contributor], ignoreOrder: true);
    }
}
