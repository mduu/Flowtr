using Flowtr.Shared;

namespace Flowtr.Server.CrossCutting.Cqs;

/// <summary>
/// Handles one query (read-only use case). Returns <see cref="Result{T}"/>.
/// </summary>
public interface IQueryHandler<in TQuery, TResult>
{
    Task<Result<TResult>> Handle(TQuery query, CancellationToken cancellationToken);
}
