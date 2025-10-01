using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

class UdpChatServer
{
    private const int port = 9000;
    private UdpClient? server;
    private ConcurrentDictionary<IPEndPoint, bool> clients = new();
    private Dictionary<IPEndPoint, (string Nick, string Color)> clientInfo = new();
    private int clientCounter = 0;

    public async Task StartAsync()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "СЕРВЕРНА СТОРОНА";
        await InitializeServerAsync();

        await ReceiveMessagesAsync();
    }

    private async Task InitializeServerAsync()
    {
        while (true)
        {
            try
            {
                server = new UdpClient(port);
                Console.WriteLine($"сервер запущено на порту {port}.");
                break;
            }
            catch (SocketException)
            {
                Console.WriteLine("не вдалося запустити сервер. очікування повторного підключення...");
                await Task.Delay(1000);
            }
        }
    }

    private async Task ReceiveMessagesAsync()
    {
        while (true)
        {
            var result = await server.ReceiveAsync();
            var message = Encoding.UTF8.GetString(result.Buffer);

            if (message == "off")
            {
                clients.TryRemove(result.RemoteEndPoint, out _);
                clientInfo.Remove(result.RemoteEndPoint);
                Console.WriteLine($"\nклієнт від'єднався: {result.RemoteEndPoint}");
                continue;
            }

            if (message.StartsWith("init|"))
            {
                var parts = message.Split('|');
                if (parts.Length == 3)
                    HandleInitMessage(result.RemoteEndPoint, parts[1], parts[2]);
                continue;
            }

            if (message.StartsWith("msg|"))
            {
                var parts = message.Split('|', 3);
                if (parts.Length == 3)
                {
                    var color = parts[1];
                    var text = parts[2];
                    var formattedMessage = $"{color}|{text}";
                    ConsoleWriteColored(color, text);
                    await BroadcastMessageAsync(formattedMessage, result.RemoteEndPoint);
                }
            }
        }
    }

    private void HandleInitMessage(IPEndPoint client, string nick, string color)
    {
        clientInfo[client] = (nick, color);
        clients[client] = true;
        clientCounter++;
        Console.WriteLine($"\nклієнт підключився: {nick} ({client}) з кольором {color}");
    }

    private void ConsoleWriteColored(string color, string text)
    {
        if (Enum.TryParse(color, out ConsoleColor parsedColor))
            Console.ForegroundColor = parsedColor;

        Console.WriteLine(text);
        Console.ResetColor();
    }

    private async Task BroadcastMessageAsync(string message, IPEndPoint? excludeClient = null)
    {
        var data = Encoding.UTF8.GetBytes(message);
        foreach (var client in clients.Keys)
        {
            if (!client.Equals(excludeClient))
                await server.SendAsync(data, data.Length, client);
        }
    }

    static async Task Main() => await new UdpChatServer().StartAsync();
}