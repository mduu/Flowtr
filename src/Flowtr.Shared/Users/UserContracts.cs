namespace Flowtr.Shared.Users;

/// <summary>
/// A user in the user list. <see cref="InvitationOpen"/> is true until the user accepts the invitation.
/// </summary>
public sealed record UserSummary(string Id, string Email, IReadOnlyList<string> Roles, bool InvitationOpen, DateTimeOffset InvitedAt);

public sealed record InviteUserRequest(string Email, string Role);

public sealed record ChangeRolesRequest(IReadOnlyList<string> Roles);

/// <summary>
/// The link with which the invited user completes the registration.
/// </summary>
public sealed record Invitation(string UserId, string Email, string Link);
