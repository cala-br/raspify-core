using SpotifyAPI.Web;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace RaspifyCore
{
    static class SpotifyAPIExtensions
    {
        public static async Task<PKCETokenResponse> LoadTokenAsync(string path)
        {
            var json = await File.ReadAllTextAsync(path);
            var token =
                JsonSerializer.Deserialize<PKCETokenResponse>(json);

            return token;
        }

        public static async Task SaveAsync(this PKCETokenResponse token, string path)
        {
            var serializedToken =
                JsonSerializer.Serialize(token);

            await File.WriteAllTextAsync(path, serializedToken);
        }
    }
}
