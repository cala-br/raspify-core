using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

#nullable enable

namespace RaspifyCore
{
    static class SpotifyApiExtension
    {
        public static async Task<PKCETokenResponse> LoadTokenAsync(string path)
        {
            var json = await File.ReadAllTextAsync(path);
            var token =
                JsonSerializer.Deserialize<PKCETokenResponse>(json);

            if (token is null)
                throw new JsonException($"Couldn't load a token from {path}");

            return token;
        }

        public static async Task SaveAsync(this PKCETokenResponse token, string path)
        {
            var serializedToken =
                JsonSerializer.Serialize(token);

            await File.WriteAllTextAsync(path, serializedToken);
        }


        public static async Task<SpotifyClient> CreateSpotifyClientAsync(string clientId, string credentialsPath)
        {
            using var rAuth = new RaspifyAuthentication(clientId, credentialsPath);
            var authenticator = await rAuth.GetAuthenticatorAsync();

            var config = SpotifyClientConfig
                .CreateDefault()
                .WithAuthenticator(authenticator);

            return new SpotifyClient(config);
        }


        public static List<string> GetArtistNames(this FullTrack track)
        {
            return track
                .Artists
                .Select(artist => artist.Name)
                .ToList();
        }


        public static async Task DownloadAsync(this Image image, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException($"{nameof(path)} must have a value");

            using var client = new WebClient();
            await Task.Run(() => 
            {
                client.DownloadFile(image.Url, path);
            });
        }
    }
}
