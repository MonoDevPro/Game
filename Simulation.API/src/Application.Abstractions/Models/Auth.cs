namespace Application.Models.Models;

public record RegisterCommand(string UserName, string Email, string Password) : ICommand<AuthResponse>;
public record LoginRequest(string UserNameOrEmail, string Password);
public record AuthResponse(string AccessToken);
