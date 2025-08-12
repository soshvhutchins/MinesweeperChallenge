using AutoMapper;
using Minesweeper.Application.Commands.Authentication;
using Minesweeper.Application.Common;
using Minesweeper.Application.Common.Interfaces;
using Minesweeper.Application.DTOs;
using Minesweeper.Domain.Aggregates;
using Minesweeper.Domain.Common;
using Minesweeper.Domain.Repositories;
using Minesweeper.Domain.ValueObjects;

namespace Minesweeper.Application.Handlers.Commands;

public class RegisterPlayerCommandHandler : ICommandHandler<RegisterPlayerCommand, AuthenticationResult>
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IMapper _mapper;

    public RegisterPlayerCommandHandler(
        IPlayerRepository playerRepository,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService,
        IMapper mapper)
    {
        _playerRepository = playerRepository;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
        _mapper = mapper;
    }

    public async Task<Result<AuthenticationResult>> Handle(RegisterPlayerCommand request, CancellationToken cancellationToken)
    {
        // Check if username already exists
        var existingPlayerByUsername = await _playerRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (existingPlayerByUsername != null)
            return Result.Failure<AuthenticationResult>("Username is already taken");

        // Check if email already exists
        var existingPlayerByEmail = await _playerRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingPlayerByEmail != null)
            return Result.Failure<AuthenticationResult>("Email is already registered");

        // Hash password
        var hashedPassword = _passwordService.HashPassword(request.Password);

        // Create new player
        var playerId = PlayerId.New();
        var player = Player.Create(
            playerId,
            request.Username,
            request.Email,
            hashedPassword);

        // Save player
        await _playerRepository.AddAsync(player, cancellationToken);
        await _playerRepository.SaveChangesAsync(cancellationToken);

        // Generate tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(playerId.Value, request.Username, request.Email);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var expiresAt = _jwtTokenService.GetTokenExpirationTime();

        // Map to DTO
        var playerDto = _mapper.Map<PlayerDto>(player);

        return Result.Success(new AuthenticationResult
        {
            Token = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            Player = playerDto
        });
    }
}

public class LoginPlayerCommandHandler : ICommandHandler<LoginPlayerCommand, AuthenticationResult>
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IMapper _mapper;

    public LoginPlayerCommandHandler(
        IPlayerRepository playerRepository,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService,
        IMapper mapper)
    {
        _playerRepository = playerRepository;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
        _mapper = mapper;
    }

    public async Task<Result<AuthenticationResult>> Handle(LoginPlayerCommand request, CancellationToken cancellationToken)
    {
        // Try to find player by username or email
        var player = await _playerRepository.GetByUsernameAsync(request.UsernameOrEmail, cancellationToken);

        if (player == null)
        {
            // Try by email if username lookup failed
            player = await _playerRepository.GetByEmailAsync(request.UsernameOrEmail, cancellationToken);
        }

        if (player == null)
            return Result.Failure<AuthenticationResult>("Invalid username/email or password");

        // Verify password
        if (!_passwordService.VerifyPassword(request.Password, player.PasswordHash))
            return Result.Failure<AuthenticationResult>("Invalid username/email or password");

        if (!player.IsActive)
            return Result.Failure<AuthenticationResult>("Account is deactivated");

        // Update last login time
        player.UpdateLastLogin();
        _playerRepository.Update(player);
        await _playerRepository.SaveChangesAsync(cancellationToken);

        // Generate tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(player.Id.Value, player.Username, player.Email);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var expiresAt = _jwtTokenService.GetTokenExpirationTime();

        // Map to DTO
        var playerDto = _mapper.Map<PlayerDto>(player);

        return Result.Success(new AuthenticationResult
        {
            Token = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            Player = playerDto
        });
    }
}
