using Xunit;
using FluentAssertions;
using Minesweeper.Domain.Aggregates;
using Minesweeper.Domain.ValueObjects;
using Minesweeper.Domain.Enums;

namespace Minesweeper.UnitTests.Domain;

public class GameTests
{
    [Fact]
    public void NewGame_ShouldBeInNotStartedState()
    {
        // Arrange
        var gameId = GameId.New();
        var playerId = PlayerId.New();
        var difficulty = GameDifficulty.Beginner;

        // Act
        var game = new Game(gameId, playerId, difficulty);

        // Assert
        game.Id.Should().Be(gameId);
        game.PlayerId.Should().Be(playerId);
        game.Status.Should().Be(GameStatus.NotStarted);
        game.IsFirstMove.Should().BeTrue();
        game.IsActive.Should().BeFalse();
        game.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void StartGame_WithValidFirstClick_ShouldStartGame()
    {
        // Arrange
        var gameId = GameId.New();
        var playerId = PlayerId.New();
        var difficulty = GameDifficulty.Beginner;
        var game = new Game(gameId, playerId, difficulty, new Random(42));
        var firstClickPosition = CellPosition.Of(0, 0);

        // Act
        var result = game.StartGame(firstClickPosition);

        // Assert
        result.IsSuccess.Should().BeTrue();
        game.Status.Should().Be(GameStatus.InProgress);
        game.IsFirstMove.Should().BeFalse();
        game.IsActive.Should().BeTrue();
        game.Board.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void RevealCell_WhenGameNotStarted_ShouldStartGameAndRevealCell()
    {
        // Arrange
        var gameId = GameId.New();
        var playerId = PlayerId.New();
        var difficulty = GameDifficulty.Beginner;
        var game = new Game(gameId, playerId, difficulty, new Random(42));
        var position = CellPosition.Of(4, 4); // Middle position

        // Act
        var result = game.RevealCell(position);

        // Assert
        result.IsSuccess.Should().BeTrue();
        game.Status.Should().Be(GameStatus.InProgress);
        game.IsActive.Should().BeTrue();

        var cell = game.Board.GetCell(position);
        cell.IsRevealed.Should().BeTrue();
        cell.HasMine.Should().BeFalse(); // First click should never be a mine
    }

    [Fact]
    public void ToggleFlag_OnHiddenCell_ShouldFlagCell()
    {
        // Arrange
        var gameId = GameId.New();
        var playerId = PlayerId.New();
        var difficulty = GameDifficulty.Beginner;
        var game = new Game(gameId, playerId, difficulty, new Random(42));
        var flagPosition = CellPosition.Of(1, 1);

        // Act
        var result = game.ToggleFlag(flagPosition);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var cell = game.Board.GetCell(flagPosition);
        cell.IsFlagged.Should().BeTrue();
    }

    [Fact]
    public void GetRemainingMineCount_ShouldCalculateCorrectly()
    {
        // Arrange
        var gameId = GameId.New();
        var playerId = PlayerId.New();
        var difficulty = GameDifficulty.Beginner; // 10 mines
        var game = new Game(gameId, playerId, difficulty, new Random(42));
        var startPosition = CellPosition.Of(0, 0);

        // Act
        var remainingBefore = game.GetRemainingMineCount();
        game.ToggleFlag(CellPosition.Of(1, 1));
        game.ToggleFlag(CellPosition.Of(1, 2));
        var remainingAfter = game.GetRemainingMineCount();

        // Assert
        remainingBefore.Should().Be(10);
        remainingAfter.Should().Be(8); // 10 - 2 flags = 8
    }

    [Fact]
    public void PauseGame_WhenInProgress_ShouldPauseGame()
    {
        // Arrange
        var gameId = GameId.New();
        var playerId = PlayerId.New();
        var difficulty = GameDifficulty.Beginner;
        var game = new Game(gameId, playerId, difficulty, new Random(42));

        game.StartGame(CellPosition.Of(0, 0));

        // Act
        var result = game.PauseGame();

        // Assert
        result.IsSuccess.Should().BeTrue();
        game.Status.Should().Be(GameStatus.Paused);
        game.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ResumeGame_WhenPaused_ShouldResumeGame()
    {
        // Arrange
        var gameId = GameId.New();
        var playerId = PlayerId.New();
        var difficulty = GameDifficulty.Beginner;
        var game = new Game(gameId, playerId, difficulty, new Random(42));

        game.StartGame(CellPosition.Of(0, 0));
        game.PauseGame();

        // Act
        var result = game.ResumeGame();

        // Assert
        result.IsSuccess.Should().BeTrue();
        game.Status.Should().Be(GameStatus.InProgress);
        game.IsActive.Should().BeTrue();
    }

    [Fact]
    public void GetStatistics_ShouldReturnCorrectInformation()
    {
        // Arrange
        var gameId = GameId.New();
        var playerId = PlayerId.New();
        var difficulty = GameDifficulty.Beginner;
        var game = new Game(gameId, playerId, difficulty, new Random(42));

        game.StartGame(CellPosition.Of(0, 0));

        // Act
        var stats = game.GetStatistics();

        // Assert
        stats.GameId.Should().Be(gameId);
        stats.PlayerId.Should().Be(playerId);
        stats.Difficulty.Should().Be(difficulty);
        stats.Status.Should().Be(GameStatus.InProgress);
        stats.ProgressPercentage.Should().BeGreaterThan(0);
    }
}
