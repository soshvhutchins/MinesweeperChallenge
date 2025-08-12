using AutoMapper;
using Minesweeper.Application.Commands.Games;
using Minesweeper.Application.Common;
using Minesweeper.Application.DTOs;
using Minesweeper.Domain.Common;
using Minesweeper.Domain.Repositories;
using Minesweeper.Domain.ValueObjects;

namespace Minesweeper.Application.Handlers.Commands;

public class RevealCellCommandHandler : ICommandHandler<RevealCellCommand, GameDto>
{
    private readonly IGameRepository _gameRepository;
    private readonly IMapper _mapper;

    public RevealCellCommandHandler(IGameRepository gameRepository, IMapper mapper)
    {
        _gameRepository = gameRepository;
        _mapper = mapper;
    }

    public async Task<Result<GameDto>> Handle(RevealCellCommand request, CancellationToken cancellationToken)
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

        // Reveal cell
        var revealResult = game.RevealCell(position);
        if (revealResult.IsFailure)
            return Result.Failure<GameDto>(revealResult.Error);

        // Update game
        _gameRepository.Update(game);

        // Map to DTO
        var gameDto = _mapper.Map<GameDto>(game);
        return Result.Success(gameDto);
    }
}
