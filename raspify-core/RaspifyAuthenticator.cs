using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Threading;

namespace raspify_core
{
    class RaspifyAuth : IDisposable
    {
        private static readonly Uri _serverUri = new Uri("http://localhost:5000/callback");
        private static readonly int _serverPort = 5_000;

        private readonly EmbedIOAuthServer _server = new(_serverUri, _serverPort);


        private string _clientId;
        private string _credentialsPath;


        public RaspifyAuth(string clientId, string credentialsPath)
        {
            _clientId = clientId;
            _credentialsPath = credentialsPath;
        }


        public async Task<IAuthenticator> AuthenticateAsync()
        {
            bool credentialsExist =
                File.Exists(_credentialsPath);

            if (!credentialsExist)
                await FetchCredentialsAndAuthenticateAsync();

            return await AuthenticateFromCredentialsAsync();
        }


        private async Task<IAuthenticator> AuthenticateFromCredentialsAsync()
        {
            var token = await LoadTokenAsync();
            var authenticator = new PKCEAuthenticator(_clientId, token);

            authenticator.TokenRefreshed += OnTokenRefreshed;

            return authenticator;
        }

        private async Task<PKCETokenResponse> LoadTokenAsync()
        {
            var json = await File.ReadAllTextAsync(_credentialsPath);
            var token =
                JsonSerializer.Deserialize<PKCETokenResponse>(json);
            
            return token;
        }

        private async void OnTokenRefreshed(object sender, PKCETokenResponse token)
        {
            await SaveTokenAsync(token);
        }


        private async Task FetchCredentialsAndAuthenticateAsync()
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
                Console.WriteLine("Unable to open URL, manually open: {0}", uri);
            }

            tokenReceived.WaitOne();
        }


        private async Task StartServerAsync(string verifier, AutoResetEvent tokenReceived)
        {
            await _server.Start();
            _server.AuthorizationCodeReceived += async (sender, response) =>
            {
                await OnAuthorizationCodeReceived(response, verifier);
                tokenReceived.Set();
            };
        }

        private async Task OnAuthorizationCodeReceived(AuthorizationCodeResponse response, string verifier)
        {
            await _server.Stop();
            PKCETokenResponse token = await new OAuthClient().RequestToken(
              new PKCETokenRequest(_clientId, response.Code, _server.BaseUri, verifier)
            );

            await SaveTokenAsync(token);
        }

        private LoginRequest CreateRequest(string challenge)
        {
            return new LoginRequest(_server.BaseUri, _clientId, LoginRequest.ResponseType.Code)
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


        private async Task SaveTokenAsync(PKCETokenResponse token)
        {
            var serializedToken =
                JsonSerializer.Serialize(token);

            await File.WriteAllTextAsync(
                path: _credentialsPath, 
                contents: serializedToken
            );
        }

        public void Dispose()
        {
            _server.Dispose();
        }
    }
}
