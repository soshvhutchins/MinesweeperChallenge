using Minesweeper.Application.DTOs;
using Minesweeper.Domain.Common;

namespace Minesweeper.Application.Common.Interfaces;

/// <summary>
/// Service for JWT token generation and validation
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT token for the authenticated user
    /// </summary>
    string GenerateAccessToken(Guid playerId, string username, string email);

    /// <summary>
    /// Generates a refresh token
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a JWT token and returns the player ID if valid
    /// </summary>
    Result<Guid> ValidateToken(string token);

    /// <summary>
    /// Gets the expiration time for access tokens
    /// </summary>
    DateTime GetTokenExpirationTime();
}

/// <summary>
/// Service for password operations
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// Hashes a password using BCrypt
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against its hash
    /// </summary>
    bool VerifyPassword(string password, string hash);
}
