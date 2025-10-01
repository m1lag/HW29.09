using System.Net;
using System.Net.Sockets;
using System.Text;

class UdpChatClient
{
    private const int serverPort = 9000;
    private UdpClient? client;
    private IPEndPoint? serverEndpoint;
    private string nickname = "";
    private ConsoleColor userColor = ConsoleColor.White;

    public async Task StartAsync()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "КЛІЄНТСЬКА СТОРОНА";
        var serverIp = "127.0.0.1";
        serverEndpoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);

        client = new UdpClient(0);
        await ConnectToServerAsync();

        AppDomain.CurrentDomain.ProcessExit += async (s, e) => await SendDisconnectMessageAsync();

        _ = Task.Run(ReceiveMessagesAsync);
        await SendMessagesAsync();
    }

    private async Task ConnectToServerAsync()
    {
        while (true)
        {
            try
            {
                client.Connect(serverEndpoint);
                await SendInitialMessageAsync();
                Console.WriteLine("Підключено до сервера.");
                break;
            }
            catch (SocketException)
            {
                Console.WriteLine("Очікування підключення до сервера...");
                await Task.Delay(1000);
            }
        }
    }

    private async Task SendInitialMessageAsync()
    {
        Console.Write("Введіть ваш нік: ");
        nickname = Console.ReadLine() ?? "Гість";

        userColor = ChooseColor();

        var initialMessage = $"init|{nickname}|{userColor}";
        var data = Encoding.UTF8.GetBytes(initialMessage);
        await client.SendAsync(data, data.Length);
    }

    private ConsoleColor ChooseColor()
    {
        Console.Write("Kолір (Red, Green, Yellow): ");
        var input = Console.ReadLine();
        if (Enum.TryParse(input, out ConsoleColor color))
            return color;
        return ConsoleColor.White;
    }

    private async Task SendDisconnectMessageAsync()
    {
        var disconnectMessage = "off";
        var data = Encoding.UTF8.GetBytes(disconnectMessage);
        await client.SendAsync(data, data.Length);
    }

    private async Task ReceiveMessagesAsync()
    {
        while (true)
        {
            var result = await client.ReceiveAsync();
            var message = Encoding.UTF8.GetString(result.Buffer);

            var parts = message.Split('|');
            if (parts.Length == 2)
            {
                if (Enum.TryParse(parts[0], out ConsoleColor color))
                    Console.ForegroundColor = color;

                Console.WriteLine(parts[1]);
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine(message);
            }
        }
    }

    private async Task SendMessagesAsync()
    {
        while (true)
        {
            Console.Write("Надішліть повідомлення на сервер: ");
            var text = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(text)) continue;

            var formattedMessage = FormatMessage(text);
            var data = Encoding.UTF8.GetBytes(formattedMessage);
            await client.SendAsync(data, data.Length);

            if (text == "off")
                break;
        }

        client.Close();
        Console.WriteLine("Відключено від сервера.");
    }

    private string FormatMessage(string text)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        return $"msg|{userColor}|[{timestamp}] {nickname}: {text}";
    }

    static async Task Main() => await new UdpChatClient().StartAsync();
}