using Game.Contracts;
using Game.Infrastructure.LiteNetLib;
using Server.Host.WorldServer;

namespace Server.Host.ChatServer;

public class ChatServerWorker(NetServer server) : BackgroundService
{
    const int ChatPort = 9052;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        server.EnvelopeReceived += (peer, envelope) =>
        {
            if (envelope.OpCode != OpCode.ChatSendRequest)
            {
                return;
            }

            if (envelope.Payload is not ChatSendRequest request)
            {
                return;
            }

            var message = new ChatMessage(request.Channel, request.Sender, request.Message);
            server.SendToAll(new Envelope(OpCode.ChatMessage, message));
        };

        server.Start(ChatPort);
        Console.WriteLine($"ChatServer listening on {ChatPort}");

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