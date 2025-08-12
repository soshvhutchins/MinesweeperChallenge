namespace Minesweeper.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identifier for players
/// </summary>
public record PlayerId(Guid Value)
{
    public static PlayerId New() => new(Guid.NewGuid());

    public static PlayerId From(Guid value) => new(value);

    public static implicit operator Guid(PlayerId playerId) => playerId.Value;
    public static implicit operator PlayerId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
