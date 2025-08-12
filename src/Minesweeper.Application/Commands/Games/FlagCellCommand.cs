using FluentValidation;
using Minesweeper.Application.Common;
using Minesweeper.Application.DTOs;

namespace Minesweeper.Application.Commands.Games;

public record FlagCellCommand : ICommand<GameDto>
{
    public Guid GameId { get; init; }
    public Guid PlayerId { get; init; }
    public int Row { get; init; }
    public int Column { get; init; }
}

public class FlagCellCommandValidator : AbstractValidator<FlagCellCommand>
{
    public FlagCellCommandValidator()
    {
        RuleFor(x => x.GameId)
            .NotEmpty()
            .WithMessage("Game ID is required");

        RuleFor(x => x.PlayerId)
            .NotEmpty()
            .WithMessage("Player ID is required");

        RuleFor(x => x.Row)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Row must be non-negative");

        RuleFor(x => x.Column)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Column must be non-negative");
    }
}
