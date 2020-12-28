using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace RaspifyCore
{
    class InBrowserAuthentication : IDisposable
    {
        private static readonly Uri _serverUri = new Uri("http://localhost:5000/callback");
        private static readonly int _serverPort = 5_000;

        private readonly Lazy<EmbedIOAuthServer> _server = new(() => new(_serverUri, _serverPort));
        private EmbedIOAuthServer Server => _server.Value;

        private readonly string _clientId;
        private readonly string _credentialsPath;

        private readonly AutoResetEvent _tokenReceived = new(initialState: false);


        public InBrowserAuthentication(string clientId, string credentialsPath)
        {
            _clientId = clientId;
            _credentialsPath = credentialsPath;
        }


        public async Task FetchCredentialsAsync()
        {
            var (verifier, challenge) = PKCEUtil.GenerateCodes();
            await StartServerAsync(verifier);
            
            var uri = GetRequestUri(challenge);
            TryOpenBrowser(uri);

            _tokenReceived.WaitOne();
        }


        private async Task StartServerAsync(string verifier)
        {
            await Server.Start();
            Server.AuthorizationCodeReceived += async (sender, response) =>
            {
                await OnAuthorizationCodeReceived(response, verifier);
                _tokenReceived.Set();
            };
        }

        private async Task OnAuthorizationCodeReceived(AuthorizationCodeResponse response, string verifier)
        {
            await Server.Stop();
            var token = await RequestTokenAsync(response.Code, verifier);

            await token.SaveAsync(_credentialsPath);
        }

        private async Task<PKCETokenResponse> RequestTokenAsync(string code, string verifier)
        {
            var oAuthClient = new OAuthClient();
            var tokenRequest = new PKCETokenRequest(
                code: code,
                clientId: _clientId,
                redirectUri: Server.BaseUri,
                codeVerifier: verifier
            );
            
            return await oAuthClient.RequestToken(tokenRequest);
        }

        private Uri GetRequestUri(string challenge)
        {
            var request = CreateRequest(challenge);
            var uri = request.ToUri();
            return uri;
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


        private static void TryOpenBrowser(Uri uri)
        {
            try {
                BrowserUtil.Open(uri);
            }
            catch {
                ConsoleUI
                    .GetInstance()
                    .PushLogMessage($"Unable to open URL, manually open: {uri}");
            }
        }


        public void Dispose()
        {
            if (_server.IsValueCreated)
                Server.Dispose();

            _tokenReceived.Dispose();
        }
    }
}
