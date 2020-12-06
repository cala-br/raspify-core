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

#nullable enable

namespace RaspifyCore
{
    class RaspifyServer : IDisposable
    {
        private ConcurrentBag<StreamWriter> _activeServerStreams = new();
        private CancellationTokenSource _tokenSource = new();


        public event EventHandler? ClientConnected;


        public async Task SendAllAsync(string message)
        {
            foreach(var stream in _activeServerStreams)
            {
                await stream.WriteAsync(message + '\0');
                await stream.FlushAsync();
            }
        }


        public async void Start()
        {
            await Task.Run(StartNewPipeServer, _tokenSource.Token);
        }


        private async void StartNewPipeServer()
        {
            using var server =
                new NamedPipeServerStream("raspify_pipe", PipeDirection.InOut);

            await server.WaitForConnectionAsync(_tokenSource.Token);
            using var outStream = new StreamWriter(server);

            _tokenSource.Token.ThrowIfCancellationRequested();
            _activeServerStreams.Add(outStream);
            ClientConnected?.Invoke(this, null!);

            while (true)
            {
                if (_tokenSource.Token.IsCancellationRequested)
                {
                    Console.WriteLine("Cancelled");
                    return;
                }
            }
        }

        public void Dispose()
        {
            _tokenSource.Cancel();
            _tokenSource.Dispose();
        }
    }
}
