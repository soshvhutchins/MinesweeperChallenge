using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minesweeper.Application.Commands.Games;
using Minesweeper.Application.DTOs;
using Minesweeper.Application.Queries.Games;
using Minesweeper.Domain.ValueObjects;
using System.Security.Claims;

namespace Minesweeper.WebApi.Controllers;

/// <summary>
/// Game management endpoints for Minesweeper
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class GamesController : ControllerBase
{
    private readonly ILogger<GamesController> _logger;
    private readonly IMediator _mediator;

    public GamesController(ILogger<GamesController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    /// <summary>
    /// Gets the authenticated player ID from the JWT token
    /// </summary>
    /// <returns>Player ID from the current user context</returns>
    private PlayerId GetAuthenticatedPlayerId()
    {
        var playerIdClaim = User.FindFirst("playerId")?.Value
            ?? throw new UnauthorizedAccessException("Player ID not found in token");

        if (!Guid.TryParse(playerIdClaim, out var playerGuid))
            throw new UnauthorizedAccessException("Invalid player ID format in token");

        return new PlayerId(playerGuid);
    }

    /// <summary>
    /// Creates a new minesweeper game
    /// </summary>
    /// <param name="request">Game creation parameters</param>
    /// <returns>Created game information</returns>
    [HttpPost]
    [ProducesResponseType<GameDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GameDto>> CreateGame([FromBody] CreateGameRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var playerId = GetAuthenticatedPlayerId();

            var command = new StartNewGameCommand
            {
                PlayerId = playerId.Value,
                DifficultyName = request.DifficultyName
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return BadRequest(new { error = result.Error });
            }

            return CreatedAtAction(nameof(GetGame), new { id = result.Value.Id }, result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating game");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a specific game by ID
    /// </summary>
    /// <param name="id">Game ID</param>
    /// <returns>Game information</returns>
    [HttpGet("{id}")]
    [ProducesResponseType<GameDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameDto>> GetGame(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var playerId = GetAuthenticatedPlayerId();

            var query = new GetGameByIdQuery
            {
                GameId = id,
                PlayerId = playerId.Value
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (result.IsFailure)
            {
                return NotFound(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting game {GameId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Reveals a cell in the game
    /// </summary>
    /// <param name="id">Game ID</param>
    /// <param name="request">Cell reveal parameters</param>
    /// <returns>Updated game state</returns>
    [HttpPost("{id}/reveal")]
    [ProducesResponseType<GameDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameDto>> RevealCell(Guid id, [FromBody] RevealCellRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var playerId = GetAuthenticatedPlayerId();

            var command = new RevealCellCommand
            {
                GameId = id,
                PlayerId = playerId.Value,
                Row = request.Row,
                Column = request.Column
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revealing cell in game {GameId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Toggles a flag on a cell
    /// </summary>
    /// <param name="id">Game ID</param>
    /// <param name="request">Cell flag parameters</param>
    /// <returns>Updated game state</returns>
    [HttpPost("{id}/flag")]
    [ProducesResponseType<GameDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameDto>> ToggleFlag(Guid id, [FromBody] FlagCellRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var playerId = GetAuthenticatedPlayerId();

            var command = new FlagCellCommand
            {
                GameId = id,
                PlayerId = playerId.Value,
                Row = request.Row,
                Column = request.Column
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flagging cell in game {GameId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets available game difficulties
    /// </summary>
    /// <returns>List of available difficulties</returns>
    [HttpGet("difficulties")]
    [ProducesResponseType<IEnumerable<object>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<object>> GetDifficulties()
    {
        var difficulties = GameDifficulty.GetPredefinedDifficulties()
            .Select(d => new
            {
                d.Name,
                d.Rows,
                d.Columns,
                d.MineCount,
                d.TotalCells,
                d.SafeCells,
                MineDensity = Math.Round(d.MineDensity, 1)
            });

        return Ok(difficulties);
    }

    /// <summary>
    /// Gets all active games for the current player
    /// </summary>
    /// <returns>List of player's games</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GameDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GameDto>>> GetActiveGames(CancellationToken cancellationToken)
    {
        try
        {
            var playerId = GetAuthenticatedPlayerId();

            var query = new GetActiveGamesQuery
            {
                PlayerId = playerId.Value
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (result.IsFailure)
            {
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active games");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

}

// Request Models for backward compatibility

/// <summary>
/// Request model for creating a new game
/// </summary>
public record CreateGameRequest(string DifficultyName);

/// <summary>
/// Request model for revealing a cell
/// </summary>
public record RevealCellRequest(int Row, int Column);

/// <summary>
/// Request model for flagging a cell
/// </summary>
public record FlagCellRequest(int Row, int Column);
