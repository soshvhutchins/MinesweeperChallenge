using FluentValidation;
using Minesweeper.Application.Common;
using Minesweeper.Application.DTOs;

namespace Minesweeper.Application.Queries.Games;

public record GetGameByIdQuery : IQuery<GameDto>
{
    public Guid GameId { get; init; }
    public Guid PlayerId { get; init; }
}

public class GetGameByIdQueryValidator : AbstractValidator<GetGameByIdQuery>
{
    public GetGameByIdQueryValidator()
    {
        RuleFor(x => x.GameId)
            .NotEmpty()
            .WithMessage("Game ID is required");

        RuleFor(x => x.PlayerId)
            .NotEmpty()
            .WithMessage("Player ID is required");
    }
}

public record GetActiveGamesQuery : IQuery<IReadOnlyList<GameDto>>
{
    public Guid PlayerId { get; init; }
}

public class GetActiveGamesQueryValidator : AbstractValidator<GetActiveGamesQuery>
{
    public GetActiveGamesQueryValidator()
    {
        RuleFor(x => x.PlayerId)
            .NotEmpty()
            .WithMessage("Player ID is required");
    }
}

public record GetGameHistoryQuery : IQuery<IReadOnlyList<GameDto>>
{
    public Guid PlayerId { get; init; }
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 50;
}

public class GetGameHistoryQueryValidator : AbstractValidator<GetGameHistoryQuery>
{
    public GetGameHistoryQueryValidator()
    {
        RuleFor(x => x.PlayerId)
            .NotEmpty()
            .WithMessage("Player ID is required");

        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Skip must be non-negative");

        RuleFor(x => x.Take)
            .GreaterThan(0)
            .LessThanOrEqualTo(100)
            .WithMessage("Take must be between 1 and 100");
    }
}

public record GetLeaderboardQuery : IQuery<IReadOnlyList<PlayerStatisticsDto>>
{
    public string DifficultyName { get; init; } = string.Empty;
    public int Take { get; init; } = 10;
}

public class GetLeaderboardQueryValidator : AbstractValidator<GetLeaderboardQuery>
{
    public GetLeaderboardQueryValidator()
    {
        RuleFor(x => x.DifficultyName)
            .NotEmpty()
            .WithMessage("Difficulty name is required");

        RuleFor(x => x.Take)
            .GreaterThan(0)
            .LessThanOrEqualTo(50)
            .WithMessage("Take must be between 1 and 50");
    }
}

public record GetDifficultiesQuery : IQuery<IReadOnlyList<GameDifficultyDto>>
{
}

public record GetGameStatisticsQuery : IQuery<GameStatisticsDto>
{
    public Guid GameId { get; init; }
    public Guid PlayerId { get; init; }
}

public class GetGameStatisticsQueryValidator : AbstractValidator<GetGameStatisticsQuery>
{
    public GetGameStatisticsQueryValidator()
    {
        RuleFor(x => x.GameId)
            .NotEmpty()
            .WithMessage("Game ID is required");

        RuleFor(x => x.PlayerId)
            .NotEmpty()
            .WithMessage("Player ID is required");
    }
}
