using System.Security.Claims;

namespace Flowtr.Server.CrossCutting.Audit;

/// <summary>
/// Who makes a change: the signed-in user, <c>Anonym</c> for a request without sign-in
/// (for example accepting an invitation), or <c>System</c> without a request (jobs, start).
/// </summary>
public interface IAuditActor
{
    (string? UserId, string Name) Current { get; }
}

public sealed class AuditActor(IHttpContextAccessor httpContextAccessor) : IAuditActor
{
    public const string Anonymous = "Anonym";

    public const string System = "System";

    public (string? UserId, string Name) Current => httpContextAccessor.HttpContext?.User switch
    {
        null => (null, System),
        { Identity.IsAuthenticated: true } user => (user.FindFirstValue(ClaimTypes.NameIdentifier), user.Identity.Name ?? "?"),
        _ => (null, Anonymous),
    };
}
