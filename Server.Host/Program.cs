using Server.Host.AuthServer;
using Server.Host.ChatServer;
using Server.Host.WorldServer;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddAuthServerServices("Data Source=game_database.db", "auth")
    .AddChatServerServices("Data Source=game_database.db", "chat")
    .AddWorldServerServices("Data Source=game_database.db", "world");

var host = builder.Build();
host.Run();