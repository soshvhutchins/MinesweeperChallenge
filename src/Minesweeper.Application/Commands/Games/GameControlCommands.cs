using FluentValidation;
using Minesweeper.Application.Common;
using Minesweeper.Application.DTOs;

namespace Minesweeper.Application.Commands.Games;

public record PauseGameCommand : ICommand<GameDto>
{
    public Guid GameId { get; init; }
    public Guid PlayerId { get; init; }
}

public class PauseGameCommandValidator : AbstractValidator<PauseGameCommand>
{
    public PauseGameCommandValidator()
    {
        RuleFor(x => x.GameId)
            .NotEmpty()
            .WithMessage("Game ID is required");

        RuleFor(x => x.PlayerId)
            .NotEmpty()
            .WithMessage("Player ID is required");
    }
}

public record ResumeGameCommand : ICommand<GameDto>
{
    public Guid GameId { get; init; }
    public Guid PlayerId { get; init; }
}

public class ResumeGameCommandValidator : AbstractValidator<ResumeGameCommand>
{
    public ResumeGameCommandValidator()
    {
        RuleFor(x => x.GameId)
            .NotEmpty()
            .WithMessage("Game ID is required");

        RuleFor(x => x.PlayerId)
            .NotEmpty()
            .WithMessage("Player ID is required");
    }
}
