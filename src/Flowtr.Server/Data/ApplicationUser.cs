using Microsoft.AspNetCore.Identity;

namespace Flowtr.Server.Data;

public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// When an admin invited the user or made a new link. Flowtr deletes open invitations after 30 days.
    /// </summary>
    public DateTimeOffset InvitedAt { get; set; }
}
