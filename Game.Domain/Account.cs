namespace Game.Domain;

public sealed record Account(int Id, string Username, string Email, string PasswordHash);