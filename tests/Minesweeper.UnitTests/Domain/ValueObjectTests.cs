using Xunit;
using FluentAssertions;
using Minesweeper.Domain.ValueObjects;

namespace Minesweeper.UnitTests.Domain;

public class ValueObjectTests
{
    [Fact]
    public void CellPosition_Create_WithValidCoordinates_ShouldSucceed()
    {
        // Act
        var result = CellPosition.Create(5, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Row.Should().Be(5);
        result.Value.Column.Should().Be(10);
    }

    [Fact]
    public void CellPosition_Create_WithNegativeRow_ShouldFail()
    {
        // Act
        var result = CellPosition.Create(-1, 5);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Row cannot be negative");
    }

    [Fact]
    public void CellPosition_GetAdjacentPositions_ShouldReturnCorrectPositions()
    {
        // Arrange
        var position = CellPosition.Of(2, 2);

        // Act
        var adjacent = position.GetAdjacentPositions().ToList();

        // Assert
        adjacent.Should().HaveCount(8);
        adjacent.Should().Contain(CellPosition.Of(1, 1));
        adjacent.Should().Contain(CellPosition.Of(1, 2));
        adjacent.Should().Contain(CellPosition.Of(1, 3));
        adjacent.Should().Contain(CellPosition.Of(2, 1));
        adjacent.Should().Contain(CellPosition.Of(2, 3));
        adjacent.Should().Contain(CellPosition.Of(3, 1));
        adjacent.Should().Contain(CellPosition.Of(3, 2));
        adjacent.Should().Contain(CellPosition.Of(3, 3));
    }

    [Theory]
    [InlineData("Beginner", 9, 9, 10)]
    [InlineData("Intermediate", 16, 16, 40)]
    [InlineData("Expert", 16, 30, 99)]
    public void GameDifficulty_PredefinedDifficulties_ShouldHaveCorrectValues(
        string name, int rows, int columns, int mineCount)
    {
        // Act
        var difficulty = GameDifficulty.FromName(name);

        // Assert
        difficulty.Should().NotBeNull();
        difficulty!.Name.Should().Be(name);
        difficulty.Rows.Should().Be(rows);
        difficulty.Columns.Should().Be(columns);
        difficulty.MineCount.Should().Be(mineCount);
    }

    [Fact]
    public void GameDifficulty_CreateCustom_WithValidParameters_ShouldSucceed()
    {
        // Act
        var result = GameDifficulty.CreateCustom("Test", 10, 15, 20);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Test");
        result.Value.Rows.Should().Be(10);
        result.Value.Columns.Should().Be(15);
        result.Value.MineCount.Should().Be(20);
        result.Value.TotalCells.Should().Be(150);
        result.Value.SafeCells.Should().Be(130);
    }

    [Fact]
    public void GameDifficulty_CreateCustom_WithTooManyMines_ShouldFail()
    {
        // Act
        var result = GameDifficulty.CreateCustom("Test", 3, 3, 9); // 9 mines in 9 cells

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Must have at least one safe cell");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void GameDifficulty_CreateCustom_WithInvalidName_ShouldFail(string? name)
    {
        // Act
        var result = GameDifficulty.CreateCustom(name, 10, 10, 15);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Difficulty name cannot be empty");
    }

    [Fact]
    public void GameId_New_ShouldCreateUniqueIds()
    {
        // Act
        var id1 = GameId.New();
        var id2 = GameId.New();

        // Assert
        id1.Should().NotBe(id2);
        id1.Value.Should().NotBe(Guid.Empty);
        id2.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void PlayerId_From_ShouldCreateFromGuid()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var playerId = PlayerId.From(guid);

        // Assert
        playerId.Value.Should().Be(guid);
    }
}
