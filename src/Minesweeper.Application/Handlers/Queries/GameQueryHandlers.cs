using AutoMapper;
using Minesweeper.Application.Common;
using Minesweeper.Application.DTOs;
using Minesweeper.Application.Queries.Games;
using Minesweeper.Domain.Common;
using Minesweeper.Domain.Repositories;
using Minesweeper.Domain.ValueObjects;

namespace Minesweeper.Application.Handlers.Queries;

public class GetGameByIdQueryHandler : IQueryHandler<GetGameByIdQuery, GameDto>
{
    private readonly IGameRepository _gameRepository;
    private readonly IMapper _mapper;

    public GetGameByIdQueryHandler(IGameRepository gameRepository, IMapper mapper)
    {
        _gameRepository = gameRepository;
        _mapper = mapper;
    }

    public async Task<Result<GameDto>> Handle(GetGameByIdQuery request, CancellationToken cancellationToken)
    {
        var gameId = GameId.From(request.GameId);
        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);

        if (game is null)
            return Result.Failure<GameDto>("Game not found");

        // Verify player ownership
        var playerId = PlayerId.From(request.PlayerId);
        if (game.PlayerId != playerId)
            return Result.Failure<GameDto>("Player is not authorized to access this game");

        var gameDto = _mapper.Map<GameDto>(game);
        return Result.Success(gameDto);
    }
}

public class GetActiveGamesQueryHandler : IQueryHandler<GetActiveGamesQuery, IReadOnlyList<GameDto>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IMapper _mapper;

    public GetActiveGamesQueryHandler(IGameRepository gameRepository, IMapper mapper)
    {
        _gameRepository = gameRepository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<GameDto>>> Handle(GetActiveGamesQuery request, CancellationToken cancellationToken)
    {
        var playerId = PlayerId.From(request.PlayerId);
        var games = await _gameRepository.GetActiveGamesByPlayerIdAsync(playerId, cancellationToken);

        var gameDtos = _mapper.Map<IReadOnlyList<GameDto>>(games);
        return Result.Success(gameDtos);
    }
}

public class GetGameHistoryQueryHandler : IQueryHandler<GetGameHistoryQuery, IReadOnlyList<GameDto>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IMapper _mapper;

    public GetGameHistoryQueryHandler(IGameRepository gameRepository, IMapper mapper)
    {
        _gameRepository = gameRepository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<GameDto>>> Handle(GetGameHistoryQuery request, CancellationToken cancellationToken)
    {
        var playerId = PlayerId.From(request.PlayerId);
        var games = await _gameRepository.GetCompletedGamesByPlayerIdAsync(
            playerId,
            request.Skip,
            request.Take,
            cancellationToken);

        var gameDtos = _mapper.Map<IReadOnlyList<GameDto>>(games);
        return Result.Success(gameDtos);
    }
}

public class GetLeaderboardQueryHandler : IQueryHandler<GetLeaderboardQuery, IReadOnlyList<PlayerStatisticsDto>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IMapper _mapper;

    public GetLeaderboardQueryHandler(IGameRepository gameRepository, IMapper mapper)
    {
        _gameRepository = gameRepository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<PlayerStatisticsDto>>> Handle(GetLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var statistics = await _gameRepository.GetLeaderboardAsync(
            request.DifficultyName,
            request.Take,
            cancellationToken);

        var statisticsDtos = _mapper.Map<IReadOnlyList<PlayerStatisticsDto>>(statistics);
        return Result.Success(statisticsDtos);
    }
}

public class GetDifficultiesQueryHandler : IQueryHandler<GetDifficultiesQuery, IReadOnlyList<GameDifficultyDto>>
{
    private readonly IMapper _mapper;

    public GetDifficultiesQueryHandler(IMapper mapper)
    {
        _mapper = mapper;
    }

    public Task<Result<IReadOnlyList<GameDifficultyDto>>> Handle(GetDifficultiesQuery request, CancellationToken cancellationToken)
    {
        var difficulties = GameDifficulty.GetPredefinedDifficulties();
        var difficultyDtos = _mapper.Map<IReadOnlyList<GameDifficultyDto>>(difficulties);

        return Task.FromResult(Result.Success(difficultyDtos));
    }
}

public class GetGameStatisticsQueryHandler : IQueryHandler<GetGameStatisticsQuery, GameStatisticsDto>
{
    private readonly IGameRepository _gameRepository;
    private readonly IMapper _mapper;

    public GetGameStatisticsQueryHandler(IGameRepository gameRepository, IMapper mapper)
    {
        _gameRepository = gameRepository;
        _mapper = mapper;
    }

    public async Task<Result<GameStatisticsDto>> Handle(GetGameStatisticsQuery request, CancellationToken cancellationToken)
    {
        var gameId = GameId.From(request.GameId);
        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);

        if (game is null)
            return Result.Failure<GameStatisticsDto>("Game not found");

        // Verify player ownership
        var playerId = PlayerId.From(request.PlayerId);
        if (game.PlayerId != playerId)
            return Result.Failure<GameStatisticsDto>("Player is not authorized to access this game");

        var statistics = game.GetStatistics();
        var statisticsDto = _mapper.Map<GameStatisticsDto>(statistics);

        return Result.Success(statisticsDto);
    }
}
