using System.IO;
using System.Threading.Tasks;
using System;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web;
using System.Collections.Generic;
using Newtonsoft.Json;
using static SpotifyAPI.Web.Scopes;
using RaspifyCore;

namespace Example.CLI.PersistentConfig
{
    public class Program
    {
        private const string CREDENTIALS_PATH = "credentials.json";
        private static readonly string clientId = File.ReadAllText("client_id.txt");

        public static async Task<int> Main()
        {
            if (string.IsNullOrEmpty(clientId))
            {
                throw new NullReferenceException(
                  "Please set SPOTIFY_CLIENT_ID via environment variables before starting the program"
                );
            }

            await Start();

            Console.ReadKey();
            return 0;
        }

        private static async Task Start()
        {
            using var rAuth= new RaspifyAuth(clientId, CREDENTIALS_PATH);
            var authenticator = await rAuth.GetAuthenticatorAsync();

            var config = SpotifyClientConfig
                .CreateDefault()
                .WithAuthenticator(authenticator);

            var spotify = new SpotifyClient(config);

            var me = await spotify.UserProfile.Current();

            var track = await spotify
                .Player
                .GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest { Market = "from_token" });

            Console.WriteLine($"Welcome {me.DisplayName} ({me.Id}), you're authenticated!");

            var playlists = await spotify.PaginateAll(await spotify.Playlists.CurrentUsers().ConfigureAwait(false));
            Console.WriteLine($"Total Playlists in your Account: {playlists.Count}");

            Environment.Exit(0);
        }
    }
}