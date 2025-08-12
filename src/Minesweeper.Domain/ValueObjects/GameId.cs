namespace Minesweeper.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identifier for games
/// </summary>
public record GameId(Guid Value)
{
    public static GameId New() => new(Guid.NewGuid());

    public static GameId From(Guid value) => new(value);

    public static implicit operator Guid(GameId gameId) => gameId.Value;
    public static implicit operator GameId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
