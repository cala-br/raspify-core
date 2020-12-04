using System.IO;
using System.Threading.Tasks;
using System;
using SpotifyAPI.Web;
using System.Linq;
using static RaspifyCore.SpotifyAPIExtensions;

#nullable enable

namespace RaspifyCore
{
    public class Program
    {
        static readonly string credentialsPath = "credentials.json";
        static readonly string clientId = File.ReadAllText("client_id.txt");


        public static async Task Main()
        {
            await Start();
            Console.ReadKey();
        }


        private static async Task Start()
        {
            var spotify = 
                await CreateSpotifyClientAsync(clientId, credentialsPath);

            var currentlyPlaying = await spotify
                .Player
                .GetCurrentlyPlaying(new (){ Market = "from_token" });

            TryPrintCurrentlyPlaying(currentlyPlaying);

            Environment.Exit(0);
        }


        private static void TryPrintCurrentlyPlaying(CurrentlyPlaying currentlyPlaying)
        {
            if (currentlyPlaying.Item is FullTrack track)
            {
                var progress = currentlyPlaying.ProgressMs!.Value;
                PrintTrack(track, progress);
            }
        }

        private static void PrintTrack(FullTrack track, int progress)
        {
            var artists = track
                .Artists
                .Select(artist => artist.Name);

            var albumImages = track
                .Album
                .Images
                .Select(image => $"\n\t\t\t({image.Width}, {image.Height}) | {image.Url}");

            Console.WriteLine($@"
                Name: {track.Name}
                Artists: {string.Join(", ", artists)}
                Duration: {TimeSpan.FromMilliseconds(track.DurationMs)}
                Progress: {TimeSpan.FromMilliseconds(progress)}
                    
                Album: {track.Album.Name}
                Covers: {string.Join(' ', albumImages)}
            ");
        }
    }
}