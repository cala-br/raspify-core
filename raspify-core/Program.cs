using System.IO;
using System.Threading.Tasks;
using System;
using ApiExt = RaspifyCore.SpotifyApiExtension;
using SpotifyAPI.Web;

#nullable enable

namespace RaspifyCore
{
    public class Program
    {
        static readonly string credentialsPath = "credentials.json";
        static readonly string clientId = File.ReadAllText("client_id.txt");
        static readonly ConsoleUI console = ConsoleUI.GetInstance();


        public static async Task Main()
        {
            await StartAsync();
        }


        private static async Task StartAsync()
        {
            var spotify =
                await ApiExt.CreateSpotifyClientAsync(clientId, credentialsPath);

            using var server = new RaspifyServer();
            AddServerHandlers(spotify, server);

            server.Start();
            console.PushLogMessage("Started");
            HandleConsoleCommands(server);
            console.PushLogMessage("Ended");
        }

        private static void AddServerHandlers(SpotifyClient spotify, RaspifyServer server)
        {
            server.ClientConnected += async (s, e) =>
            {
                await OnClientConnected(e, spotify, server);
            };

            server.ClientDisconnected += (s, e) =>
            {
                console.PushLogMessage($"Client disconnected {e.EndPoint}");
            };

            server.OnError += (s, msg) =>
            {
                console.PushLogMessage(msg);
            };
        }

        private static async Task OnClientConnected(ClientEventArgs e, SpotifyClient spotify, RaspifyServer server)
        {
            console.PushLogMessage($"Client connected {e.EndPoint}");

            var currentlyPlaying = await spotify
                .Player
                .GetCurrentlyPlaying(new() { Market = "from_token" });

            if (currentlyPlaying is null)
            {
                console.PushLogMessage("Nothing playing");
                return;
            }

            var track = CurrentTrack
                .From(currentlyPlaying)
                .ToString();

            await server.SendAllAsync(track);
        }
    
    
        private static void HandleConsoleCommands(RaspifyServer server)
        {
            while (true)
            {
                console.Draw();
                var key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.D:
                        console.PushLogMessage("Disconnecting clients...");
                        server.DisconnectAll();
                        break;

                    case ConsoleKey.E:
                        return;
                }
            }
        }
    }
}