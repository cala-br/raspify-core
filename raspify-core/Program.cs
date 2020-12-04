using System.IO;
using System.Threading.Tasks;
using System;
using SpotifyAPI.Web;
using System.Linq;
using ApiExt = RaspifyCore.SpotifyApiExtension;

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
                await ApiExt.CreateSpotifyClientAsync(clientId, credentialsPath);

            var currentlyPlaying = await spotify
                .Player
                .GetCurrentlyPlaying(new (){ Market = "from_token" });

            var track = CurrentTrack.From(currentlyPlaying);
            Console.WriteLine(track);

            Environment.Exit(0);
        }
    }
}