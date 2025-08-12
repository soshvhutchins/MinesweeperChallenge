using Minesweeper.Domain.Common;
using Minesweeper.Domain.ValueObjects;

namespace Minesweeper.Domain.Events;

public record GameStartedEvent(
    GameId GameId,
    PlayerId PlayerId,
    GameDifficulty Difficulty
) : DomainEvent;

public record CellRevealedEvent(
    GameId GameId,
    PlayerId PlayerId,
    CellPosition Position,
    bool HasMine,
    int AdjacentMineCount,
    IReadOnlyList<CellPosition> CascadeRevealedPositions
) : DomainEvent;

public record CellFlaggedEvent(
    GameId GameId,
    PlayerId PlayerId,
    CellPosition Position,
    bool IsFlagged
) : DomainEvent;

public record GameWonEvent(
    GameId GameId,
    PlayerId PlayerId,
    TimeSpan Duration,
    int FlagsUsed
) : DomainEvent;

public record GameLostEvent(
    GameId GameId,
    PlayerId PlayerId,
    CellPosition MinePosition,
    TimeSpan Duration
) : DomainEvent;

public record GamePausedEvent(
    GameId GameId,
    PlayerId PlayerId
) : DomainEvent;

public record GameResumedEvent(
    GameId GameId,
    PlayerId PlayerId
) : DomainEvent;
