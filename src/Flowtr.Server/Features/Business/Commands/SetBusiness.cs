using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Shared;
using Flowtr.Shared.Business;

namespace Flowtr.Server.Features.Business.Commands;

/// <summary>
/// Sets the name of the business. Empty removes it.
/// </summary>
public sealed record SetBusiness(BusinessInput Input);

public sealed class SetBusinessHandler(ApplicationDbContext db) : ICommandHandler<SetBusiness, Result>
{
    public async Task<Result> Handle(SetBusiness command, CancellationToken cancellationToken)
    {
        var name = string.IsNullOrWhiteSpace(command.Input.Name) ? null : command.Input.Name.Trim();
        if (name?.Length > BusinessInput.MaxLength)
        {
            return Result.Failure($"Der Name hat höchstens {BusinessInput.MaxLength} Zeichen.");
        }

        var business = await db.BusinessSettings.FindAsync([BusinessSettings.TheId], cancellationToken);
        if (business is null)
        {
            business = new BusinessSettings();
            db.BusinessSettings.Add(business);
        }

        business.Name = name;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
