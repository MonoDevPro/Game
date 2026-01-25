using Server.Host.AuthServer;
using Server.Host.ChatServer;
using Server.Host.Common;
using Server.Host.WorldServer;

var builder = Host.CreateApplicationBuilder(args);

var hostOptions = builder.Configuration
    .GetSection(ServerHostOptions.SectionName)
    .Get<ServerHostOptions>() ?? new ServerHostOptions();

builder.Services.AddOptions<CombatOptions>()
    .BindConfiguration(CombatOptions.SectionName);

builder.Services
    .AddAuthServerServices(hostOptions.ConnectionString, hostOptions.AuthServerKey)
    .AddChatServerServices(hostOptions.ChatServerKey)
    .AddWorldServerServices(hostOptions.ConnectionString, hostOptions.WorldServerKey);

var host = builder.Build();
host.Run();
