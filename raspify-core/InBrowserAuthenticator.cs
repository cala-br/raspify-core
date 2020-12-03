using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RaspifyCore
{
    class InBrowserAuthenticator : IDisposable
    {
        private static readonly Uri _serverUri = new Uri("http://localhost:5000/callback");
        private static readonly int _serverPort = 5_000;

        private readonly Lazy<EmbedIOAuthServer> _server = new(() => new(_serverUri, _serverPort));
        private EmbedIOAuthServer Server => _server.Value;

        private readonly string _clientId;
        private readonly string _credentialsPath;


        public InBrowserAuthenticator(string clientId, string credentialsPath)
        {
            _clientId = clientId;
            _credentialsPath = credentialsPath;
        }


        public async Task FetchCredentialsAsync()
        {
            var tokenReceived = new AutoResetEvent(false);

            var (verifier, challenge) = PKCEUtil.GenerateCodes();
            await StartServerAsync(verifier, tokenReceived);

            var request = CreateRequest(challenge);
            var uri = request.ToUri();

            try
            {
                BrowserUtil.Open(uri);
            }
            catch (Exception)
            {
                Console.WriteLine($"Unable to open URL, manually open: {uri}");
            }

            tokenReceived.WaitOne();
        }


        private async Task StartServerAsync(string verifier, AutoResetEvent tokenReceived)
        {
            await Server.Start();
            Server.AuthorizationCodeReceived += async (sender, response) =>
            {
                await OnAuthorizationCodeReceived(response, verifier);
                tokenReceived.Set();
            };
        }

        private async Task OnAuthorizationCodeReceived(AuthorizationCodeResponse response, string verifier)
        {
            await Server.Stop();
            var token = await new OAuthClient().RequestToken(
              new PKCETokenRequest(_clientId, response.Code, Server.BaseUri, verifier)
            );

            await token.SaveAsync(_credentialsPath);
        }

        private LoginRequest CreateRequest(string challenge)
        {
            return new LoginRequest(Server.BaseUri, _clientId, LoginRequest.ResponseType.Code)
            {
                CodeChallenge = challenge,
                CodeChallengeMethod = "S256",
                Scope = new List<string> {
                    Scopes.UserReadEmail,
                    Scopes.UserReadPrivate,
                    Scopes.PlaylistReadPrivate,
                    Scopes.PlaylistReadCollaborative,
                    Scopes.UserReadCurrentlyPlaying,
                    Scopes.UserReadPlaybackState,
                },
            };
        }


        public void Dispose()
        {
            if (_server.IsValueCreated)
                Server.Dispose();
        }
    }
}
