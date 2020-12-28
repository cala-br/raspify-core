using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Pipes;
using System.Threading.Tasks;
using ApiExt = RaspifyCore.SpotifyApiExtension;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using System.Net.Sockets;
using System.Net;

#nullable enable

namespace RaspifyCore
{
    class RaspifyServer : IDisposable
    {
        private List<Socket> _activeClients = new();
        private CancellationTokenSource _tokenSource = new();
        
        public event EventHandler<ClientEventArgs>? ClientDisconnected;
        public event EventHandler<ClientEventArgs>? ClientConnected;
        public event EventHandler<string>? OnError;


        public void Start()
        {
            SpawnNew();
        }

        private async void SpawnNew()
        {
            try {
                await Task.Run(StartServer, _tokenSource.Token);
            }
            catch (Exception e) {
                OnError?.Invoke(this, e.Message);
            }
        }

        private async Task StartServer()
        {
            using var server = 
                new Socket(SocketType.Stream, ProtocolType.Tcp);
            
            server.Bind(new IPEndPoint(IPAddress.Any, 50_000));
            server.Listen();

            var token = _tokenSource.Token;
            while (true)
            {
                var client = await server.AcceptAsync();
                token.ThrowIfCancellationRequested();

                _ = Task.Run(() => HandleClient(client));
            }
        }


        private void HandleClient(Socket client)
        {
            var ep = client.RemoteEndPoint.Clone();
            OnClientConnected(client, ep);

            bool shouldSpin() =>
                client.Connected &&
                !_tokenSource.IsCancellationRequested;

            while (shouldSpin())
                ;

            OnClientDisconnected(client, ep);
            client.Dispose();
        }

        private void OnClientConnected(Socket client, EndPoint endPoint)
        {
            lock (_activeClients)
                _activeClients.Add(client);

            ClientConnected?.Invoke(this, new(endPoint));
        }

        private void OnClientDisconnected(Socket client, EndPoint endPoint)
        {
            lock (_activeClients)
                _activeClients.Remove(client);

            ClientDisconnected?.Invoke(this, new(endPoint));
        }


        public async Task SendAllAsync(string message)
        {
            foreach (var client in _activeClients.ToList())
                await SendOneAsync(client, message);
        }


        private async Task SendOneAsync(Socket client, string message)
        {
            try
            {
                using var netStream = new NetworkStream(client);
                using var outStream = new StreamWriter(netStream);

                var messageLength = message.Length.ToString("0000");

                await outStream.WriteAsync(messageLength);
                await outStream.WriteAsync(message);
                await outStream.FlushAsync();
            }
            catch
            {
                OnError?.Invoke(this, $"Error when sending to {client.RemoteEndPoint}");
            }
        }


        public void DisconnectAll()
        {
            lock (_activeClients)
            {
                _activeClients.ForEach(c => c.Disconnect(reuseSocket: false));
                _activeClients.Clear();
            }
        }


        public void Dispose()
        {
            _tokenSource.Cancel();
            _tokenSource.Dispose();
        }
    }


    class ClientEventArgs : EventArgs
    {
        public EndPoint EndPoint { get; init; }

        public ClientEventArgs(EndPoint endPoint)
        {
            if (endPoint is null)
                throw new ArgumentNullException(nameof(endPoint));

            EndPoint = endPoint;
        }
    }
}
