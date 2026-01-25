using Game.Application;
using Game.Contracts;
using Game.Infrastructure.LiteNetLib;
using Game.Infrastructure.EfCore;

namespace Server.Host.AuthServer;

public class AuthServerWorker(IServiceScopeFactory scopeFactory, NetServer server) : BackgroundService
{
    const int AuthPort = 9050;
    const string WorldEndpoint = "127.0.0.1:9051";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            await DbInitializer.EnsureCreatedAndSeedAsync(db, stoppingToken);
        }

        server.PeerConnected += peer =>
            scopeFactory.CreateScope().ServiceProvider
                .GetRequiredService<ILoggerFactory>().CreateLogger("Auth")
                .LogInformation("Peer connected {PeerId}", peer.Id);
        server.PeerDisconnected += (peer, info) =>
            scopeFactory.CreateScope().ServiceProvider
                .GetRequiredService<ILoggerFactory>().CreateLogger("Auth")
                .LogInformation("Peer disconnected {PeerId} {Reason}", peer.Id, info.Reason);

        server.EnvelopeReceived += async (peer, envelope) =>
        {
            using var scope = scopeFactory.CreateScope();
            var auth = scope.ServiceProvider.GetRequiredService<AuthUseCases>();

            switch (envelope.OpCode)
            {
                case OpCode.AuthLoginRequest:
                {
                    if (envelope.Payload is not AuthLoginRequest request)
                    {
                        server.Send(peer,
                            new Envelope(OpCode.AuthLoginResponse,
                                new AuthLoginResponse(false, null, "Invalid payload")));
                        return;
                    }

                    var result = await auth.LoginAsync(request, stoppingToken);
                    server.Send(peer, new Envelope(OpCode.AuthLoginResponse, result.Success
                        ? result.Value
                        : new AuthLoginResponse(false, null, result.Error)));
                    break;
                }
                case OpCode.AuthCharacterListRequest:
                {
                    if (envelope.Payload is not CharacterListRequest request)
                    {
                        server.Send(peer,
                            new Envelope(OpCode.AuthCharacterListResponse,
                                new CharacterListResponse(false, "Invalid payload", new())));
                        return;
                    }

                    var result = await auth.ListCharactersAsync(request, stoppingToken);
                    server.Send(peer, new Envelope(OpCode.AuthCharacterListResponse, result.Success
                        ? result.Value
                        : new CharacterListResponse(false, result.Error, new())));
                    break;
                }
                case OpCode.AuthSelectCharacterRequest:
                {
                    if (envelope.Payload is not SelectCharacterRequest request)
                    {
                        server.Send(peer,
                            new Envelope(OpCode.AuthSelectCharacterResponse,
                                new SelectCharacterResponse(false, "Invalid payload", null, null)));
                        return;
                    }

                    var result = await auth.SelectCharacterAsync(request, WorldEndpoint, stoppingToken);
                    server.Send(peer, new Envelope(OpCode.AuthSelectCharacterResponse, result.Success
                        ? result.Value
                        : new SelectCharacterResponse(false, result.Error, null, null)));
                    break;
                }
            }
        };

        server.Start(AuthPort);
        Console.WriteLine($"AuthServer listening on {AuthPort}");

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        while (!cts.IsCancellationRequested)
        {
            server.PollEvents();
            await Task.Delay(15, cts.Token).ContinueWith(_ => { });
        }

        server.Stop();
    }
}