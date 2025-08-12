using FluentValidation;
using Minesweeper.Application.Common;
using Minesweeper.Application.DTOs;

namespace Minesweeper.Application.Commands.Games;

public record StartNewGameCommand : ICommand<GameDto>
{
    public Guid PlayerId { get; init; }
    public string DifficultyName { get; init; } = string.Empty;
    public int? CustomRows { get; init; }
    public int? CustomColumns { get; init; }
    public int? CustomMineCount { get; init; }
}

public class StartNewGameCommandValidator : AbstractValidator<StartNewGameCommand>
{
    public StartNewGameCommandValidator()
    {
        RuleFor(x => x.PlayerId)
            .NotEmpty()
            .WithMessage("Player ID is required");

        RuleFor(x => x.DifficultyName)
            .NotEmpty()
            .WithMessage("Difficulty name is required")
            .Must(BeValidDifficulty)
            .WithMessage("Invalid difficulty name. Valid options: Beginner, Intermediate, Expert, Custom");

        When(x => x.DifficultyName.Equals("Custom", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.CustomRows)
                .NotNull()
                .GreaterThan(0)
                .LessThanOrEqualTo(50)
                .WithMessage("Custom rows must be between 1 and 50");

            RuleFor(x => x.CustomColumns)
                .NotNull()
                .GreaterThan(0)
                .LessThanOrEqualTo(50)
                .WithMessage("Custom columns must be between 1 and 50");

            RuleFor(x => x.CustomMineCount)
                .NotNull()
                .GreaterThan(0)
                .WithMessage("Custom mine count must be greater than 0");

            RuleFor(x => x)
                .Must(HaveValidMineToCellRatio)
                .WithMessage("Mine count must be less than total cells and leave at least one safe cell");
        });
    }

    private static bool BeValidDifficulty(string difficultyName)
    {
        var validDifficulties = new[] { "Beginner", "Intermediate", "Expert", "Custom" };
        return validDifficulties.Contains(difficultyName, StringComparer.OrdinalIgnoreCase);
    }

    private static bool HaveValidMineToCellRatio(StartNewGameCommand command)
    {
        if (!command.CustomRows.HasValue || !command.CustomColumns.HasValue || !command.CustomMineCount.HasValue)
            return false;

        var totalCells = command.CustomRows.Value * command.CustomColumns.Value;
        return command.CustomMineCount.Value < totalCells - 1; // Leave at least one safe cell
    }
}
