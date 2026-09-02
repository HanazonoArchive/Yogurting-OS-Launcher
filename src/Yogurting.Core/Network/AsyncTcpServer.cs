using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Yogurting.Core.Network
{
    /// <summary>
    /// High-performance asynchronous TCP server with 4-byte length header packet framing.
    /// Eliminates TCP stream fragmentation issues.
    /// </summary>
    public sealed class AsyncTcpServer
    {
        private readonly TcpListener _listener;
        private readonly ConcurrentDictionary<Guid, ClientSession> _sessions;
        private readonly CancellationTokenSource _cts;
        private bool _isRunning;

        public string ServerName { get; }
        public int Port { get; }
        public int ActiveConnections => _sessions.Count;

        public event Func<ClientSession, Task>? ClientConnected;
        public event Func<ClientSession, byte[], Task>? PacketReceived;
        public event Func<ClientSession, Task>? ClientDisconnected;

        public AsyncTcpServer(string name, IPAddress address, int port)
        {
            ServerName = name;
            Port = port;
            _listener = new TcpListener(address, port);
            _sessions = new ConcurrentDictionary<Guid, ClientSession>();
            _cts = new CancellationTokenSource();
        }

        public void Start()
        {
            if (_isRunning) return;
            
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listener.Start(backlog: 100);
            _isRunning = true;

            Console.WriteLine($"[{ServerName}] Listening on port {Port} (TCP Async)");
            _ = AcceptLoopAsync(_cts.Token);
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;
            _cts.Cancel();
            _listener.Stop();

            foreach (var session in _sessions.Values)
            {
                session.Disconnect();
            }
            _sessions.Clear();
            Console.WriteLine($"[{ServerName}] Server stopped.");
        }

        private async Task AcceptLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync(token).ConfigureAwait(false);
                    client.NoDelay = true;

                    var session = new ClientSession(client, this);
                    _sessions[session.Id] = session;

                    _ = HandleClientAsync(session, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        Console.WriteLine($"[{ServerName}] Accept error: {ex.Message}");
                    }
                }
            }
        }

        private async Task HandleClientAsync(ClientSession session, CancellationToken token)
        {
            if (ClientConnected != null)
            {
                await ClientConnected.Invoke(session).ConfigureAwait(false);
            }

            var stream = session.Client.GetStream();
            using var memoryBuffer = new MemoryStream();
            byte[] readBuffer = new byte[4096];

            try
            {
                while (!token.IsCancellationRequested && session.Client.Connected)
                {
                    int bytesRead = await stream.ReadAsync(readBuffer.AsMemory(0, readBuffer.Length), token).ConfigureAwait(false);
                    if (bytesRead <= 0) break; // Disconnected

                    memoryBuffer.Write(readBuffer, 0, bytesRead);

                    // Process complete packets from accumulated memory buffer
                    byte[] accumulated = memoryBuffer.ToArray();
                    int processedOffset = 0;

                    while (accumulated.Length - processedOffset >= 6)
                    {
                        int payloadLen = BitConverter.ToInt32(accumulated, processedOffset);
                        if (payloadLen < 0 || payloadLen > 65535)
                        {
                            // Drain invalid data
                            processedOffset = accumulated.Length;
                            break;
                        }

                        int totalPacketLen = 6 + payloadLen;
                        if (accumulated.Length - processedOffset < totalPacketLen)
                        {
                            // Need more bytes to complete packet
                            break;
                        }

                        byte[] packetData = new byte[totalPacketLen];
                        Buffer.BlockCopy(accumulated, processedOffset, packetData, 0, totalPacketLen);
                        processedOffset += totalPacketLen;

                        if (PacketReceived != null)
                        {
                            ushort opcode = BitConverter.ToUInt16(packetData, 4);
                            string opName = Enum.IsDefined(typeof(PacketOpcode), (PacketOpcode)opcode) ? ((PacketOpcode)opcode).ToString() : $"0x{opcode:X4}";
                            Yogurting.Core.Logging.Logger.Packet(ServerName, $"<- RECV [{session.RemoteEndPoint}]", opcode, opName, packetData.Length, packetData);
                            
                            await PacketReceived.Invoke(session, packetData).ConfigureAwait(false);
                        }
                    }

                    // Keep unparsed trailing bytes in stream
                    if (processedOffset > 0)
                    {
                        int remaining = accumulated.Length - processedOffset;
                        memoryBuffer.SetLength(0);
                        if (remaining > 0)
                        {
                            memoryBuffer.Write(accumulated, processedOffset, remaining);
                        }
                    }
                }
            }
            catch
            {
                // Disconnected
            }
            finally
            {
                _sessions.TryRemove(session.Id, out _);
                session.Dispose();

                if (ClientDisconnected != null)
                {
                    await ClientDisconnected.Invoke(session).ConfigureAwait(false);
                }
            }
        }
    }

    /// <summary>
    /// Represents a connected game client session.
    /// </summary>
    public sealed class ClientSession : IDisposable
    {
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        public Guid Id { get; } = Guid.NewGuid();
        public TcpClient Client { get; }
        public AsyncTcpServer Server { get; }
        public IPEndPoint RemoteEndPoint => (IPEndPoint)Client.Client.RemoteEndPoint!;
        public string? AccountId { get; set; }
        public string? CharacterName { get; set; }
        public int CharaId { get; set; } = 1;
        public int SessionKey { get; set; } = 0;
        public bool IsConnected => Client != null && Client.Connected;

        public ClientSession(TcpClient client, AsyncTcpServer server)
        {
            Client = client;
            Server = server;
        }

        public async Task SendAsync(byte[] data)
        {
            if (!Client.Connected) return;

            await _sendLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!Client.Connected) return;

                if (data.Length >= 6)
                {
                    ushort opcode = BitConverter.ToUInt16(data, 4);
                    string opName = Enum.IsDefined(typeof(PacketOpcode), (PacketOpcode)opcode) ? ((PacketOpcode)opcode).ToString() : $"0x{opcode:X4}";
                    Yogurting.Core.Logging.Logger.Packet(Server.ServerName, $"-> SEND [{RemoteEndPoint}]", opcode, opName, data.Length, data);
                }
                await Client.GetStream().WriteAsync(data.AsMemory()).ConfigureAwait(false);
            }
            catch
            {
                Disconnect();
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public void Disconnect()
        {
            try
            {
                Client.Close();
            }
            catch { }
        }

        public void Dispose()
        {
            Client.Dispose();
            _sendLock.Dispose();
        }
    }
}
