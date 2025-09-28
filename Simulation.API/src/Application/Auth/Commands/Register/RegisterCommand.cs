using Application.Models;
using GameWeb.Application.Common.Interfaces;
using ValidationException = GameWeb.Application.Common.Exceptions.ValidationException;

namespace GameWeb.Application.Auth.Commands.Register;

public record RegisterCommand(string UserName, string Email, string Password) : ICommand<AuthResponse>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    private readonly IIdentityService _identityService;

    public RegisterCommandValidator(IIdentityService identityService)
    {
        _identityService = identityService;

        RuleFor(v => v.UserName)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters long.")
            .MaximumLength(30).WithMessage("Username must not exceed 30 characters.")
            .Matches("^[a-zA-Z][a-zA-Z0-9_]*$").WithMessage("Username must start with a letter and can only contain letters, numbers, and underscores.")
            .MustAsync(BeUniqueUserName).WithMessage("The specified username is already in use.");

        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not a valid email address.")
            .MustAsync(BeUniqueEmail).WithMessage("The specified email is already in use.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Password is required.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
    }

    private async Task<bool> BeUniqueUserName(string userName, CancellationToken cancellationToken)
    {
        return !await _identityService.IsUserNameInUseAsync(userName, cancellationToken);
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        return !await _identityService.IsEmailInUseAsync(email, cancellationToken);
    }
}

// O handler contém a lógica de negócio
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;

    public RegisterCommandHandler(IIdentityService identityService, ITokenService tokenService)
    {
        _identityService = identityService;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var (result, userId) = await _identityService.CreateUserAsync(request.UserName, request.Email, request.Password, cancellationToken);

        if (!result.Succeeded)
            throw new ValidationException(result.Errors.Select(e => new FluentValidation.Results.ValidationFailure("register", e)));

        var accessToken = await _tokenService.IssueAccessTokenAsync(userId, cancellationToken);

        return new AuthResponse(accessToken);
    }
}
