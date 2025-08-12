using MediatR;
using Minesweeper.Domain.Common;

namespace Minesweeper.Application.Common;

/// <summary>
/// Base interface for commands that return a Result
/// </summary>
public interface ICommand : IRequest<Result>
{
}

/// <summary>
/// Base interface for commands that return a Result with value
/// </summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}

/// <summary>
/// Base interface for queries
/// </summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}

/// <summary>
/// Base interface for command handlers
/// </summary>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{
}

/// <summary>
/// Base interface for command handlers with response
/// </summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}

/// <summary>
/// Base interface for query handlers
/// </summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
