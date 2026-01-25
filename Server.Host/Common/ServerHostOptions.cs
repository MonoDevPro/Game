namespace Server.Host.Common;

public sealed class ServerHostOptions
{
    public const string SectionName = "ServerHost";

    public string ConnectionString { get; set; } = "Data Source=game_database.db";
    public string AuthServerKey { get; set; } = "auth";
    public string ChatServerKey { get; set; } = "chat";
    public string WorldServerKey { get; set; } = "world";
}
