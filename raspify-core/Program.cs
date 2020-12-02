using System.IO;
using System.Threading.Tasks;
using System;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web;
using System.Collections.Generic;
using Newtonsoft.Json;
using static SpotifyAPI.Web.Scopes;
using raspify_core;

namespace Example.CLI.PersistentConfig
{
    public class Program
    {
        private const string CredentialsPath = "credentials.json";
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

            //if (File.Exists(CredentialsPath))
            //{
            //    await Start();
            //}
            //else
            //{
            //    await StartAuthentication();
            //}

            Console.ReadKey();
            return 0;
        }

        private static async Task Start()
        {
            //var json = await File.ReadAllTextAsync(CredentialsPath);
            //var token = JsonConvert.DeserializeObject<PKCETokenResponse>(json);

            //var authenticator = new PKCEAuthenticator(clientId!, token);
            //authenticator.TokenRefreshed += (sender, token) =>
            //{
            //    File.WriteAllText(CredentialsPath, JsonConvert.SerializeObject(token));
            //};

            using var rAuth= new RaspifyAuth(clientId, CredentialsPath);
            var authenticator = await rAuth.AuthenticateAsync();

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

        //private static async Task StartAuthentication()
        //{
        //    var (verifier, challenge) = PKCEUtil.GenerateCodes();

        //    await _server.Start();
        //    _server.AuthorizationCodeReceived += async (sender, response) =>
        //    {
        //        await _server.Stop();
        //        PKCETokenResponse token = await new OAuthClient().RequestToken(
        //          new PKCETokenRequest(clientId!, response.Code, _server.BaseUri, verifier)
        //        );

        //        await File.WriteAllTextAsync(CredentialsPath, JsonConvert.SerializeObject(token));
        //        await Start();
        //    };

        //    var request = new LoginRequest(_server.BaseUri, clientId!, LoginRequest.ResponseType.Code)
        //    {
        //        CodeChallenge = challenge,
        //        CodeChallengeMethod = "S256",
        //        Scope = new List<string> { UserReadEmail, UserReadPrivate, PlaylistReadPrivate, PlaylistReadCollaborative, UserReadCurrentlyPlaying, UserReadPlaybackState }
        //    };

        //    Uri uri = request.ToUri();
        //    try
        //    {
        //        BrowserUtil.Open(uri);
        //    }
        //    catch (Exception)
        //    {
        //        Console.WriteLine("Unable to open URL, manually open: {0}", uri);
        //    }
        //}
    }
}