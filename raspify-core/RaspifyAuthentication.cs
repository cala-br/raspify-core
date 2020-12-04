using SpotifyAPI.Web;
using System;
using System.IO;
using System.Threading.Tasks;
using ApiExt = RaspifyCore.SpotifyApiExtension;

#nullable enable

namespace RaspifyCore
{
    class RaspifyAuthentication : IDisposable
    {
        private readonly InBrowserAuthentication _inBrowserAuthenticator;

        private readonly string _clientId;
        private readonly string _credentialsPath;


        public RaspifyAuthentication(string clientId, string credentialsPath)
        {
            _clientId = clientId;
            _credentialsPath = credentialsPath;
            _inBrowserAuthenticator = new(_clientId, _credentialsPath);
        }


        public async Task<IAuthenticator> GetAuthenticatorAsync()
        {
            bool credentialsExist =
                File.Exists(_credentialsPath);

            if (!credentialsExist)
                await _inBrowserAuthenticator.FetchCredentialsAsync();

            return await AuthenticateFromCredentialsAsync();
        }


        private async Task<IAuthenticator> AuthenticateFromCredentialsAsync()
        {
            var token = await ApiExt.LoadTokenAsync(_credentialsPath);
            var authenticator = new PKCEAuthenticator(_clientId, token);

            authenticator.TokenRefreshed += OnTokenRefreshed!;

            return authenticator;
        }

        
        private async void OnTokenRefreshed(object sender, PKCETokenResponse token)
        {
            await token.SaveAsync(_credentialsPath);
        }


        public void Dispose()
        {
            _inBrowserAuthenticator.Dispose();
        }
    }
}
