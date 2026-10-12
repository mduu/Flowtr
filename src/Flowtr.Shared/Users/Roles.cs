namespace Flowtr.Shared.Users;

/// <summary>
/// The roles of Flowtr (RBAC). A user can have more than one role.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";

    public const string Contributor = "Contributor";

    /// <summary>
    /// For <c>[Authorize(Roles = ...)]</c>: a user must have one of the roles to see data.
    /// </summary>
    public const string Any = Admin + "," + Contributor;

    public static readonly IReadOnlyList<string> All = [Admin, Contributor];
}
