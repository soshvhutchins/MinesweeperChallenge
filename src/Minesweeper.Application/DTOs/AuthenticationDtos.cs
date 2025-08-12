namespace Minesweeper.Application.DTOs;

/// <summary>
/// Result of authentication operations (login, register)
/// </summary>
public record AuthenticationResult
{
    public string Token { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public PlayerDto Player { get; init; } = new();
}

/// <summary>
/// Player information DTO
/// </summary>
public record PlayerDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime LastLoginAt { get; init; }
    public bool IsActive { get; init; } = true;
    public PlayerStatisticsDto Statistics { get; init; } = new();
}

/// <summary>
/// Token refresh request DTO
/// </summary>
public record RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}

/// <summary>
/// Login request DTO
/// </summary>
public record LoginRequest
{
    public string UsernameOrEmail { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// Register request DTO
/// </summary>
public record RegisterRequest
{
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
