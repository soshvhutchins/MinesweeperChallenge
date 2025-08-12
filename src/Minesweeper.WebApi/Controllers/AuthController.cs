using MediatR;
using Microsoft.AspNetCore.Mvc;
using Minesweeper.Application.Commands.Authentication;
using Minesweeper.Application.DTOs;

namespace Minesweeper.WebApi.Controllers;

/// <summary>
/// Authentication endpoints for player registration and login
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly IMediator _mediator;

    public AuthController(ILogger<AuthController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    /// <summary>
    /// Register a new player account
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <returns>Authentication result with JWT token</returns>
    [HttpPost("register")]
    [ProducesResponseType<AuthenticationResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthenticationResult>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new RegisterPlayerCommand
            {
                Username = request.Username,
                Email = request.Email,
                Password = request.Password
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                // Check if it's a conflict (username/email already exists)
                if (result.Error.Contains("already taken") || result.Error.Contains("already registered"))
                {
                    return Conflict(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("Player registered successfully: {Username}", request.Username);
            return CreatedAtAction(nameof(Login), result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during player registration");
            return StatusCode(500, new { error = "Registration failed. Please try again later." });
        }
    }

    /// <summary>
    /// Login with username/email and password
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication result with JWT token</returns>
    [HttpPost("login")]
    [ProducesResponseType<AuthenticationResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticationResult>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new LoginPlayerCommand
            {
                UsernameOrEmail = request.UsernameOrEmail,
                Password = request.Password
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                // Don't reveal specific authentication failure reasons for security
                _logger.LogWarning("Login attempt failed for user: {UsernameOrEmail}", request.UsernameOrEmail);
                return Unauthorized(new { error = "Invalid credentials" });
            }

            _logger.LogInformation("Player logged in successfully: {UsernameOrEmail}", request.UsernameOrEmail);
            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during player login");
            return StatusCode(500, new { error = "Login failed. Please try again later." });
        }
    }

    /// <summary>
    /// Logout (primarily for client-side token clearing)
    /// </summary>
    /// <returns>Success message</returns>
    [HttpPost("logout")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    public ActionResult<object> Logout()
    {
        // In a JWT-based system, logout is typically handled client-side by discarding the token
        // This endpoint exists for consistency and could be enhanced with token blacklisting in the future
        return Ok(new { message = "Logged out successfully" });
    }
}
