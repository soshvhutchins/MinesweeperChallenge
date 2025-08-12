using AutoMapper;
using Minesweeper.Application.Commands.Games;
using Minesweeper.Application.Common;
using Minesweeper.Application.DTOs;
using Minesweeper.Domain.Aggregates;
using Minesweeper.Domain.Common;
using Minesweeper.Domain.Repositories;
using Minesweeper.Domain.ValueObjects;

namespace Minesweeper.Application.Handlers.Commands;

public class StartNewGameCommandHandler : ICommandHandler<StartNewGameCommand, GameDto>
{
    private readonly IGameRepository _gameRepository;
    private readonly IMapper _mapper;

    public StartNewGameCommandHandler(IGameRepository gameRepository, IMapper mapper)
    {
        _gameRepository = gameRepository;
        _mapper = mapper;
    }

    public async Task<Result<GameDto>> Handle(StartNewGameCommand request, CancellationToken cancellationToken)
    {
        // Create difficulty
        var difficultyResult = CreateDifficulty(request);
        if (difficultyResult.IsFailure)
            return Result.Failure<GameDto>(difficultyResult.Error);

        var difficulty = difficultyResult.Value;

        // Create new game
        var gameId = GameId.New();
        var playerId = PlayerId.From(request.PlayerId);
        var game = new Game(gameId, playerId, difficulty);

        // Save game
        await _gameRepository.AddAsync(game, cancellationToken);
        await _gameRepository.SaveChangesAsync(cancellationToken);

        // Map to DTO
        var gameDto = _mapper.Map<GameDto>(game);
        return Result.Success(gameDto);
    }

    private static Result<GameDifficulty> CreateDifficulty(StartNewGameCommand request)
    {
        return request.DifficultyName.ToLowerInvariant() switch
        {
            "beginner" => Result.Success(GameDifficulty.Beginner),
            "intermediate" => Result.Success(GameDifficulty.Intermediate),
            "expert" => Result.Success(GameDifficulty.Expert),
            "custom" => CreateCustomDifficulty(request),
            _ => Result.Failure<GameDifficulty>("Invalid difficulty name")
        };
    }

    private static Result<GameDifficulty> CreateCustomDifficulty(StartNewGameCommand request)
    {
        if (!request.CustomRows.HasValue || !request.CustomColumns.HasValue || !request.CustomMineCount.HasValue)
        {
            return Result.Failure<GameDifficulty>("Custom difficulty requires rows, columns, and mine count");
        }

        return GameDifficulty.CreateCustom(
            "Custom",
            request.CustomRows.Value,
            request.CustomColumns.Value,
            request.CustomMineCount.Value);
    }
}
