using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using UnityEngine;
using System.Net;
using System;

/// <summary>
/// Підтримує декілька підключень для Хоста, і одне для Клієнта.
/// Хост-гравець створює два екземпляри: один як сервер (StartHostAsync), 
/// інший як клієнт (StartClientAsync на 127.0.0.1).
/// </summary>
public class SocketPeer : IDisposable {
    private TcpListener listener;
    private CancellationTokenSource mainCts;
    
    // Для клієнта
    private ConnectedPeer localClientPeer;
    
    // Для сервера
    private readonly ConcurrentDictionary<int, ConnectedPeer> connectedClients = new ConcurrentDictionary<int, ConnectedPeer>();
    private int nextClientId = 1;

    // === ПОДІЇ СЕРВЕРА ===
    public event Action OnHosted;
    public event Action<int> OnServerClientConnected;
    public event Action<int> OnServerClientDisconnected;
    public event Action<int, string> OnRequest;

    // === ПОДІЇ КЛІЄНТА ===
    public event Action OnClientConnected;
    public event Action OnClientDisconnected;
    public event Action<string> OnResponse;

    // ==========================================
    // СЕРВЕРНА ЧАСТИНА
    // ==========================================
    public async Task StartHostAsync(int port) {
        mainCts = new CancellationTokenSource();
        listener = new TcpListener(IPAddress.Any, port);

        try { listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); } 
        catch { /* ignore */ }

        listener.Start();
        UnityMainThreadDispatcher.Enqueue(() => OnHosted?.Invoke());

        // Запускаємо цикл прийняття клієнтів у фоні (не блокуємо цей метод)
        _ = Task.Run(() => AcceptClientsLoop(mainCts.Token), mainCts.Token);
    }

    private async Task AcceptClientsLoop(CancellationToken token) {
        try {
            while (!token.IsCancellationRequested) {
                var client = await listener.AcceptTcpClientAsync();
                try { client.NoDelay = true; } catch { }

                int clientId = nextClientId++;
                
                // Створюємо новий об'єкт для роботи з конкретним клієнтом
                var peer = new ConnectedPeer(client, clientId, 
                    onMessage: (msg) => UnityMainThreadDispatcher.Enqueue(() => OnRequest?.Invoke(clientId, msg)),
                    onDisconnected: () => {
                        connectedClients.TryRemove(clientId, out _);
                        UnityMainThreadDispatcher.Enqueue(() => OnServerClientDisconnected?.Invoke(clientId));
                    });

                connectedClients.TryAdd(clientId, peer);
                UnityMainThreadDispatcher.Enqueue(() => OnServerClientConnected?.Invoke(clientId));

                peer.StartLoops();
            }
        }
        catch (OperationCanceledException) { /* Зупинено */ }
        catch (Exception ex) { Debug.LogWarning("[SocketPeer] Accept loop error: " + ex.Message); }
    }

    public void ServerBroadcast(string message) {
        foreach (var client in connectedClients.Values) {
            client.Send(message);
        }
    }

    public void ServerSendTo(int clientId, string message) {
        if (connectedClients.TryGetValue(clientId, out var client)) {
            client.Send(message);
        }
    }

    // ==========================================
    // КЛІЄНТСЬКА ЧАСТИНА
    // ==========================================
    public async Task StartClientAsync(string host, int port, int connectTimeoutMs = 8000) {
        mainCts = new CancellationTokenSource();
        var tcpClient = new TcpClient();
        
        var connectTask = tcpClient.ConnectAsync(host, port);
        var timeoutTask = Task.Delay(connectTimeoutMs, mainCts.Token);
        var completed = await Task.WhenAny(connectTask, timeoutTask);
        
        if (completed != connectTask) {
            tcpClient.Close();
            UnityMainThreadDispatcher.Enqueue(() => Debug.LogWarning("[SocketPeer] Connect timeout"));
            return;
        }

        localClientPeer = new ConnectedPeer(tcpClient, 0, 
            onMessage: (msg) => UnityMainThreadDispatcher.Enqueue(() => OnResponse?.Invoke(msg)),
            onDisconnected: () => UnityMainThreadDispatcher.Enqueue(() => OnClientDisconnected?.Invoke())
        );

        localClientPeer.StartLoops();
        UnityMainThreadDispatcher.Enqueue(() => OnClientConnected?.Invoke());
    }

    public void ClientSend(string message) {
        localClientPeer?.Send(message);
    }

    // ==========================================
    // ОЧИЩЕННЯ
    // ==========================================
    public void CloseAll() {
        mainCts?.Cancel();

        try { listener?.Stop(); } catch { }

        localClientPeer?.Close();

        foreach (var client in connectedClients.Values) {
            client.Close();
        }
        connectedClients.Clear();
    }

    public void Dispose() {
        CloseAll();
        mainCts?.Dispose();
    }

    // ==========================================
    // ВНУТРІШНІЙ КЛАС ДЛЯ ОБРОБКИ З'ЄДНАННЯ
    // ==========================================
    private class ConnectedPeer {
        public readonly int Id;
        private readonly TcpClient client;
        private readonly NetworkStream stream;
        private readonly CancellationTokenSource peerCts;
        private readonly ConcurrentQueue<string> sendQueue = new ConcurrentQueue<string>();
        private readonly SemaphoreSlim sendSignal = new SemaphoreSlim(0);
        
        private readonly Action<string> onMessage;
        private readonly Action onDisconnected;

        public ConnectedPeer(TcpClient client, int id, Action<string> onMessage, Action onDisconnected) {
            this.client = client;
            this.Id = id;
            this.stream = client.GetStream();
            this.onMessage = onMessage;
            this.onDisconnected = onDisconnected;
            this.peerCts = new CancellationTokenSource();
        }

        public void StartLoops() {
            _ = Task.Run(ReceiveLoop, peerCts.Token);
            _ = Task.Run(SendLoop, peerCts.Token);
        }

        public void Send(string message) {
            if (string.IsNullOrEmpty(message) || peerCts.IsCancellationRequested) return;
            sendQueue.Enqueue(message);
            try { sendSignal.Release(); } catch { }
        }

        private async Task ReceiveLoop() {
            try {
                var lenBuf = new byte[4];
                while (!peerCts.IsCancellationRequested && client.Connected) {
                    int read = await ReadExactlyAsync(lenBuf, 0, 4, peerCts.Token);
                    if (read == 0) break;

                    int netlen = BitConverter.ToInt32(lenBuf, 0);
                    int len = IPAddress.NetworkToHostOrder(netlen);
                    if (len <= 0) continue;

                    var buf = new byte[len];
                    read = await ReadExactlyAsync(buf, 0, len, peerCts.Token);
                    if (read == 0) break;

                    string msg = Encoding.UTF8.GetString(buf);
                    onMessage?.Invoke(msg);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Debug.LogWarning($"[SocketPeer] Client {Id} Receive error: " + ex.Message); }
            finally { Close(); }
        }

        private async Task SendLoop() {
            try {
                while (!peerCts.IsCancellationRequested && client.Connected) {
                    await sendSignal.WaitAsync(100, peerCts.Token);

                    while (sendQueue.TryDequeue(out var msg)) {
                        var bytes = Encoding.UTF8.GetBytes(msg);
                        int netlen = IPAddress.HostToNetworkOrder(bytes.Length);
                        var lenBytes = BitConverter.GetBytes(netlen);

                        await stream.WriteAsync(lenBytes, 0, 4, peerCts.Token);
                        await stream.WriteAsync(bytes, 0, bytes.Length, peerCts.Token);
                        await stream.FlushAsync(peerCts.Token);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Debug.LogWarning($"[SocketPeer] Client {Id} Send error: " + ex.Message); }
            finally { Close(); }
        }

        private async Task<int> ReadExactlyAsync(byte[] buffer, int offset, int count, CancellationToken token) {
            int total = 0;
            while (total < count) {
                int r = await stream.ReadAsync(buffer, offset + total, count - total, token);
                if (r == 0) return 0;
                total += r;
            }
            return total;
        }

        private bool isClosed = false;
        public void Close() {
            if (isClosed) return;
            isClosed = true;

            try { peerCts.Cancel(); } catch { }
            try { stream?.Close(); } catch { }
            try { client?.Close(); } catch { }

            onDisconnected?.Invoke();
        }
    }
}