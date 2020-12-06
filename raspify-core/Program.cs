using System.IO;
using System.Threading.Tasks;
using System;
using System.IO.Pipes;
using ApiExt = RaspifyCore.SpotifyApiExtension;
using SpotifyAPI.Web;

#nullable enable

namespace RaspifyCore
{
    public class Program
    {
        static readonly string credentialsPath = "credentials.json";
        static readonly string clientId = File.ReadAllText("client_id.txt");


        public static async Task Main()
        {
            await StartAsync();
        }


        private static async Task StartAsync()
        {
            var spotify = 
                await ApiExt.CreateSpotifyClientAsync(clientId, credentialsPath);

            using var server = new RaspifyServer();

            server.ClientConnected += async (s, e) =>
            {
                Console.WriteLine("Client connected");

                var currentlyPlaying = await spotify
                    .Player
                    .GetCurrentlyPlaying(new() { Market = "from_token" });

                if (currentlyPlaying is null)
                    return;

                var track = CurrentTrack.From(currentlyPlaying);
                await server.SendAllAsync(track.ToString());

                Console.WriteLine("Sent");
            };

            server.Start();
            Console.WriteLine("Started");
            Console.ReadKey();
            Console.WriteLine("Ended");
        }
    }
}