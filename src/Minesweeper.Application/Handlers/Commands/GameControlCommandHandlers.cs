using AutoMapper;
using Minesweeper.Application.Commands.Games;
using Minesweeper.Application.Common;
using Minesweeper.Application.DTOs;
using Minesweeper.Domain.Common;
using Minesweeper.Domain.Repositories;
using Minesweeper.Domain.ValueObjects;

namespace Minesweeper.Application.Handlers.Commands;

public class FlagCellCommandHandler : ICommandHandler<FlagCellCommand, GameDto>
{
    private readonly IGameRepository _gameRepository;
    private readonly IMapper _mapper;

    public FlagCellCommandHandler(IGameRepository gameRepository, IMapper mapper)
    {
        _gameRepository = gameRepository;
        _mapper = mapper;
    }

    public async Task<Result<GameDto>> Handle(FlagCellCommand request, CancellationToken cancellationToken)
    {
        // Get game
        var gameId = GameId.From(request.GameId);
        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);

        if (game is null)
            return Result.Failure<GameDto>("Game not found");

        // Verify player ownership
        var playerId = PlayerId.From(request.PlayerId);
        if (game.PlayerId != playerId)
            return Result.Failure<GameDto>("Player is not authorized to access this game");

        // Create position
        var positionResult = CellPosition.Create(request.Row, request.Column);
        if (positionResult.IsFailure)
            return Result.Failure<GameDto>(positionResult.Error);

        var position = positionResult.Value;

        // Toggle flag
        var flagResult = game.ToggleFlag(position);
        if (flagResult.IsFailure)
            return Result.Failure<GameDto>(flagResult.Error);

        // Update game
        _gameRepository.Update(game);

        // Map to DTO
        var gameDto = _mapper.Map<GameDto>(game);
        return Result.Success(gameDto);
    }
}

public class PauseGameCommandHandler : ICommandHandler<PauseGameCommand, GameDto>
{
    private readonly IGameRepository _gameRepository;
    private readonly IMapper _mapper;

    public PauseGameCommandHandler(IGameRepository gameRepository, IMapper mapper)
    {
        _gameRepository = gameRepository;
        _mapper = mapper;
    }

    public async Task<Result<GameDto>> Handle(PauseGameCommand request, CancellationToken cancellationToken)
    {
        // Get game
        var gameId = GameId.From(request.GameId);
        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);

        if (game is null)
            return Result.Failure<GameDto>("Game not found");

        // Verify player ownership
        var playerId = PlayerId.From(request.PlayerId);
        if (game.PlayerId != playerId)
            return Result.Failure<GameDto>("Player is not authorized to access this game");

        // Pause game
        var pauseResult = game.PauseGame();
        if (pauseResult.IsFailure)
            return Result.Failure<GameDto>(pauseResult.Error);

        // Update game
        _gameRepository.Update(game);

        // Map to DTO
        var gameDto = _mapper.Map<GameDto>(game);
        return Result.Success(gameDto);
    }
}

public class ResumeGameCommandHandler : ICommandHandler<ResumeGameCommand, GameDto>
{
    private readonly IGameRepository _gameRepository;
    private readonly IMapper _mapper;

    public ResumeGameCommandHandler(IGameRepository gameRepository, IMapper mapper)
    {
        _gameRepository = gameRepository;
        _mapper = mapper;
    }

    public async Task<Result<GameDto>> Handle(ResumeGameCommand request, CancellationToken cancellationToken)
    {
        // Get game
        var gameId = GameId.From(request.GameId);
        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);

        if (game is null)
            return Result.Failure<GameDto>("Game not found");

        // Verify player ownership
        var playerId = PlayerId.From(request.PlayerId);
        if (game.PlayerId != playerId)
            return Result.Failure<GameDto>("Player is not authorized to access this game");

        // Resume game
        var resumeResult = game.ResumeGame();
        if (resumeResult.IsFailure)
            return Result.Failure<GameDto>(resumeResult.Error);

        // Update game
        _gameRepository.Update(game);

        // Map to DTO
        var gameDto = _mapper.Map<GameDto>(game);
        return Result.Success(gameDto);
    }
}
