using SpotifyAPI.Web;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

#nullable enable

namespace RaspifyCore
{
    static class SpotifyAPIExtensions
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
    }
}
