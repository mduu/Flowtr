using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Shared;

namespace Flowtr.Server.Features.Users.Queries;

/// <summary>
/// Checks an invitation link and gives the e-mail address of the invited user.
/// </summary>
public sealed record CheckInvitation(string? UserId, string? Code);

public sealed class CheckInvitationHandler(Invitations invitations) : IQueryHandler<CheckInvitation, string>
{
    public async Task<Result<string>> Handle(CheckInvitation query, CancellationToken cancellationToken)
    {
        var found = await invitations.Find(query.UserId, query.Code);

        return found.IsSuccess
            ? Result.Success(found.Value!.Email!)
            : Result.Failure<string>(found.Error!);
    }
}
