namespace Game.Contracts;

public readonly record struct ChatSendRequest(string Channel, string Sender, string Message);
public readonly record struct ChatMessage(string Channel, string Sender, string Message);
