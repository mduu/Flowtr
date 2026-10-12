using Flowtr.Server.Features.Users;
using Flowtr.Server.Features.Users.Commands;
using Flowtr.Server.Features.Users.Queries;
using Flowtr.Shared;
using Flowtr.Shared.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Flowtr.Tests.Users;

/// <summary>
/// Access only by invitation (ADR-004).
/// </summary>
public class InvitationTests
{
    [Fact]
    public async Task Invite_creates_user_without_password_with_the_role()
    {
        // Arrange
        using var fixture = new UsersFixture();

        // Act
        var invitation = await fixture.Invite("anna@example.ch", Roles.Admin);

        // Assert
        var user = (await fixture.UserManager.FindByIdAsync(invitation.UserId))!;
        user.EmailConfirmed.ShouldBeFalse();
        (await fixture.UserManager.HasPasswordAsync(user)).ShouldBeFalse();
        (await fixture.UserManager.GetRolesAsync(user)).ShouldBe([Roles.Admin]);
        user.InvitedAt.ShouldBe(fixture.Clock.Now);
        invitation.Link.ShouldStartWith("https://flowtr.test/Account/AcceptInvitation?userId=");
    }

    [Theory]
    [InlineData("keine-adresse", Roles.Contributor)]
    [InlineData("anna@example.ch", "Chef")]
    public async Task Invite_rejects_invalid_input(string email, string role)
    {
        // Arrange
        using var fixture = new UsersFixture();

        // Act
        var result = await fixture.Command<InviteUser, Result<Invitation>>(new InviteUser(email, role, UsersFixture.BaseUri));

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Invite_rejects_address_with_account()
    {
        // Arrange
        using var fixture = new UsersFixture();
        await fixture.Invite("anna@example.ch");

        // Act
        var result = await fixture.Command<InviteUser, Result<Invitation>>(new InviteUser("Anna@Example.ch", Roles.Contributor, UsersFixture.BaseUri));

        // Assert
        result.Error.ShouldBe("Für diese E-Mail-Adresse gibt es schon ein Konto oder eine Einladung.");
    }

    [Fact]
    public async Task Accept_sets_password_and_confirms_address()
    {
        // Arrange
        using var fixture = new UsersFixture();
        var invitation = await fixture.Invite("anna@example.ch");

        // Act
        var result = await fixture.Accept(invitation, "Sicher-123");

        // Assert
        result.IsSuccess.ShouldBeTrue(result.Error);
        var user = (await fixture.UserManager.FindByIdAsync(invitation.UserId))!;
        user.EmailConfirmed.ShouldBeTrue();
        (await fixture.UserManager.CheckPasswordAsync(user, "Sicher-123")).ShouldBeTrue();
    }

    [Fact]
    public async Task Accept_rejects_weak_password_and_keeps_link_valid()
    {
        // Arrange
        using var fixture = new UsersFixture();
        var invitation = await fixture.Invite("anna@example.ch");

        // Act
        var result = await fixture.Accept(invitation, "kurz");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        (await Check(fixture, invitation)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Used_link_says_already_accepted()
    {
        // Arrange
        using var fixture = new UsersFixture();
        var invitation = await fixture.Invite("anna@example.ch");
        await fixture.Accept(invitation);

        // Act
        var result = await fixture.Accept(invitation);

        // Assert
        result.Error.ShouldBe(Invitations.AlreadyAccepted);
    }

    [Fact]
    public async Task Revoked_link_says_not_found()
    {
        // Arrange
        using var fixture = new UsersFixture();
        var invitation = await fixture.Invite("anna@example.ch");

        // Act
        var revoked = await fixture.Command<RevokeInvitation, Result>(new RevokeInvitation(invitation.UserId));

        // Assert
        revoked.IsSuccess.ShouldBeTrue();
        (await Check(fixture, invitation)).Error.ShouldBe(Invitations.NotFound);
    }

    [Fact]
    public async Task Renewed_invitation_makes_old_link_invalid()
    {
        // Arrange
        using var fixture = new UsersFixture();
        var old = await fixture.Invite("anna@example.ch");
        fixture.Clock.Now = fixture.Clock.Now.AddDays(20);

        // Act
        var renewed = await fixture.Command<RenewInvitation, Result<Invitation>>(new RenewInvitation(old.UserId, UsersFixture.BaseUri));

        // Assert
        (await Check(fixture, old)).Error.ShouldBe(Invitations.LinkNotValid);
        (await Check(fixture, renewed.Value!)).Value.ShouldBe("anna@example.ch");
        (await fixture.UserManager.FindByIdAsync(old.UserId))!.InvitedAt.ShouldBe(fixture.Clock.Now);
    }

    [Fact]
    public async Task Expired_link_says_not_valid()
    {
        // Arrange
        using var fixture = new UsersFixture(configure: services =>
            services.Configure<InvitationTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromTicks(-1)));
        var invitation = await fixture.Invite("anna@example.ch");

        // Act
        var result = await Check(fixture, invitation);

        // Assert
        result.Error.ShouldBe(Invitations.LinkNotValid);
    }

    [Fact]
    public async Task Changed_code_says_not_valid()
    {
        // Arrange
        using var fixture = new UsersFixture();
        var invitation = await fixture.Invite("anna@example.ch");

        // Act
        var result = await fixture.Query<CheckInvitation, string>(new CheckInvitation(invitation.UserId, "kaputt"));

        // Assert
        result.Error.ShouldBe(Invitations.LinkNotValid);
    }

    [Fact]
    public async Task Accepted_invitation_cannot_be_revoked()
    {
        // Arrange
        using var fixture = new UsersFixture();
        var invitation = await fixture.Invite("anna@example.ch");
        await fixture.Accept(invitation);

        // Act
        var result = await fixture.Command<RevokeInvitation, Result>(new RevokeInvitation(invitation.UserId));

        // Assert
        result.IsSuccess.ShouldBeFalse();
        (await fixture.UserManager.FindByIdAsync(invitation.UserId)).ShouldNotBeNull();
    }

    private static Task<Result<string>> Check(UsersFixture fixture, Invitation invitation) =>
        fixture.Query<CheckInvitation, string>(new CheckInvitation(invitation.UserId, UsersFixture.Code(invitation)));
}
