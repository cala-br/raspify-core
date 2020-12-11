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
        private ConcurrentDictionary<Socket, Socket> _activeClients = new();
        private CancellationTokenSource _tokenSource = new();

        public event EventHandler? ClientConnected;


        public void Start()
        {
            SpawnNew();
        }

        private async void SpawnNew()
        {
            try
            {
                await Task.Run(StartNewPipeServer, _tokenSource.Token);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private async Task StartNewPipeServer()
        {
            var token = _tokenSource.Token;
            using var server =
                new Socket(SocketType.Stream, ProtocolType.Tcp);

            server.Bind(new IPEndPoint(IPAddress.Any, 50_000));
            server.Listen(1);

            var client = await server.AcceptAsync();
            token.ThrowIfCancellationRequested();

            ClientConnected?.Invoke(this, new());
            _activeClients.TryAdd(server, client);

            while (server.Connected)
                token.ThrowIfCancellationRequested();

            _activeClients.Remove(server, out var _);
            Console.WriteLine("Done");
        }


        public async Task SendAllAsync(string message)
        {
            foreach (var client in _activeClients.Values)
                await SendOneAsync(client, message);
        }


        private async Task SendOneAsync(Socket client, string message)
        {
            try
            {
                using var netStream = new NetworkStream(client);
                using var outStream = new StreamWriter(netStream);

                await outStream.WriteAsync(message + '\0');
                await outStream.FlushAsync();
            }
            catch
            {
                Console.WriteLine("Error when sending");
            }
        }


        public void Dispose()
        {
            _tokenSource.Cancel();
            _tokenSource.Dispose();
        }
    }
}
