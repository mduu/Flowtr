using Flowtr.Server.Features.Users.Commands;
using Flowtr.Shared;

namespace Flowtr.Tests.Users;

/// <summary>
/// Flowtr deletes invitations that nobody accepts within 30 days.
/// </summary>
public class ExpiredInvitationTests
{
    [Theory]
    [InlineData(30 * 24 * 60, true)]
    [InlineData(30 * 24 * 60 - 1, false)]
    public async Task Deletes_open_invitation_after_exactly_30_days(int minutesLater, bool deleted)
    {
        // Arrange
        using var fixture = new TestApp();
        var invitation = await fixture.Invite("anna@example.ch");
        fixture.Clock.Now = fixture.Clock.Now.AddMinutes(minutesLater);

        // Act
        var result = await fixture.Command<DeleteExpiredInvitations, Result<int>>(new DeleteExpiredInvitations());

        // Assert
        result.Value.ShouldBe(deleted ? 1 : 0);
        (await fixture.UserManager.FindByIdAsync(invitation.UserId) is null).ShouldBe(deleted);
    }

    [Fact]
    public async Task Keeps_accepted_user()
    {
        // Arrange
        using var fixture = new TestApp();
        var invitation = await fixture.Invite("anna@example.ch");
        await fixture.Accept(invitation);
        fixture.Clock.Now = fixture.Clock.Now.AddYears(1);

        // Act
        var result = await fixture.Command<DeleteExpiredInvitations, Result<int>>(new DeleteExpiredInvitations());

        // Assert
        result.Value.ShouldBe(0);
    }
}
