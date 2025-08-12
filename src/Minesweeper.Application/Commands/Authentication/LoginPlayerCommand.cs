using FluentValidation;
using Minesweeper.Application.Common;
using Minesweeper.Application.DTOs;

namespace Minesweeper.Application.Commands.Authentication;

public record LoginPlayerCommand : ICommand<AuthenticationResult>
{
    public string UsernameOrEmail { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public class LoginPlayerCommandValidator : AbstractValidator<LoginPlayerCommand>
{
    public LoginPlayerCommandValidator()
    {
        RuleFor(x => x.UsernameOrEmail)
            .NotEmpty()
            .WithMessage("Username or email is required");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required");
    }
}
