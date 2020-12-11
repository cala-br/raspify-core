using System.IO;
using System.Threading.Tasks;
using System;
using System.IO.Pipes;
using ApiExt = RaspifyCore.SpotifyApiExtension;
using SpotifyAPI.Web;
using System.Linq;
using System.Net;
using System.Collections.Generic;

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
            server.ClientConnected += async (_, _) =>
            {
                Console.WriteLine("Client connected");

                var currentlyPlaying = await spotify
                    .Player
                    .GetCurrentlyPlaying(new() { Market = "from_token" });

                if (currentlyPlaying is null)
                    return;

                var track =
                    CurrentTrack.From(currentlyPlaying);

                var desktopPath =
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

                var trimmedName = track
                    .Name
                    .Replace(' ', '_')
                    .Trim();

                var basePath = $@"{desktopPath}\raspify";
                var directory = Directory.CreateDirectory(basePath);

                var tasks = track
                    .AlbumImages
                    .Select(image =>
                    {
                        var savePath =
                            $@"{basePath}\{image.Width}x{image.Height}.jpeg";

                        return image.DownloadAsync(savePath);
                    });

                Task.WaitAll(tasks.ToArray());
                await server.SendAllAsync(track.ToString());
            };

            server.Start();
            Console.WriteLine("Started");
            Console.ReadKey();
            Console.WriteLine("Ended");
        }
    }
}