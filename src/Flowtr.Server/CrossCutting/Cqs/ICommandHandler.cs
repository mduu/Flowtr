using Flowtr.Shared;

namespace Flowtr.Server.CrossCutting.Cqs;

/// <summary>
/// Handles one command (use case that changes state). Returns <see cref="Result"/> or <see cref="Result{T}"/>.
/// </summary>
public interface ICommandHandler<in TCommand, TResult> where TResult : Result
{
    /// <summary>
    /// Executes the command.
    /// </summary>
    Task<TResult> Handle(TCommand command, CancellationToken cancellationToken);
}
