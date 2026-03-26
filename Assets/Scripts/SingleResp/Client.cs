using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using UnityEngine;
using System.Net;
using System;

public class Client : IDisposable {
    private CancellationTokenSource mainCts;
    private ConnectedPeer localClientPeer;

    // === ПОДІЇ КЛІЄНТА ===
    public event Action OnClientConnected;
    public event Action OnClientDisconnected;

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
            MainDispatcher.Enqueue(() => Debug.LogWarning("[SocketPeer] Connect timeout"));
            return;
        }

        localClientPeer = new ConnectedPeer(tcpClient, 0,
            onMessage: (msg) => MainDispatcher.Enqueue(() => ProcessResponse(msg)),
            onDisconnected: () => MainDispatcher.Enqueue(() => OnClientDisconnected?.Invoke())
        );

        localClientPeer.StartLoops();
        MainDispatcher.Enqueue(() => OnClientConnected?.Invoke());
    }

    public void ClientSend(byte[] message) {
        // Debug.Log($"[Client] Sending: {BitConverter.ToString(message)}");
        localClientPeer.Send(message);
    }

    // ==========================================
    // ОЧИЩЕННЯ
    // ==========================================
    public void stop() {
        localClientPeer?.Close();
    }

    public void Dispose() {
        stop();
    }

    // ==========================================
    // ВНУТРІШНІЙ КЛАС ДЛЯ ОБРОБКИ З'ЄДНАННЯ
    // ==========================================
    private class ConnectedPeer {
        public readonly int Id;
        private readonly TcpClient client;
        private readonly NetworkStream stream;
        private readonly CancellationTokenSource peerCts;
        private readonly ConcurrentQueue<byte[]> sendQueue = new ConcurrentQueue<byte[]>();
        private readonly SemaphoreSlim sendSignal = new SemaphoreSlim(0);

        private readonly Action<NetMsg> onMessage;
        private readonly Action onDisconnected;

        public ConnectedPeer(TcpClient client, int id, Action<NetMsg> onMessage, Action onDisconnected) {
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

        public void Send(byte[] message) {
            if (message.Length <= 1 || peerCts.IsCancellationRequested) return;
            sendQueue.Enqueue(message);
            try { sendSignal.Release(); } catch { }
        }

        private async Task ReceiveLoop() {
            int headerSize = Marshal.SizeOf(typeof(NetMsgHeader));
            try {
                while (!peerCts.IsCancellationRequested && client.Connected) {
                    byte[] headBuf = new byte[headerSize];
                    int read = await ReadExactlyAsync(headBuf, 0, headerSize, peerCts.Token);
                    if (read == 0) break;

                    NetMsgHeader head = NetMsgHeader.Unpack(headBuf);
                    Debug.Log($"[Client] Received message with type = {head.type}, size = {head.size}");
                    byte[] msgBuf = new byte[head.size];

                    read = await ReadExactlyAsync(msgBuf, 0, (int)head.size, peerCts.Token);
                    if (read == 0) break;

                    NetMsg msg = new NetMsg {
                        header = head,
                        payload = msgBuf,
                    };

                    onMessage.Invoke(msg);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) {
                Debug.LogWarning($"[SocketPeer] Client {Id} Receive error: " + ex.Message);
            }
            finally { Close(); }
        }

        private async Task SendLoop() {
            try {
                while (!peerCts.IsCancellationRequested && client.Connected) {
                    await sendSignal.WaitAsync(100, peerCts.Token);

                    while (sendQueue.TryDequeue(out var bytes)) {
                        await stream.WriteAsync(bytes, 0, bytes.Length, peerCts.Token);
                        await stream.FlushAsync(peerCts.Token);
                        Debug.Log("[Client] Sent message");
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


    // ==========================================
    // LOGIC
    // ==========================================
    private readonly RectTransform movableRect;
    private MapManager mapRenderer;

    public Client(RectTransform movableRect, MapManager mapper) {
        mapRenderer = mapper;
        this.movableRect = movableRect;
    }

    public void RequestMap(int w, int h) {
        NetMsg msg = new NetMsg();
        msg.payload = MapInitPayload.Pack(new MapInitPayload { w = w, h = h });
        msg.header = new NetMsgHeader {
            type = NetType.MapInit,
            size = (uint)msg.payload.Length,
        };

        Debug.Log($"[Client] size = {msg.header.size}, type = {msg.header.type}");

        ClientSend(NetMsg.Pack(msg));
    }

    public void ProcessResponse(NetMsg msg) { // <--- from server
        // some UI logic
        try {
            switch (msg.header.type) {
                case NetType.Map:
                    var mapIn = MapPayload.Unpack(msg.payload);

                    Debug.Log("[Client] Received map");
                    mapRenderer.RenderMap(mapIn);
                    break;

                default:
                    Debug.LogWarning($"[Client] Unknown server message type: {msg.header.type}");
                    break;
            }
        } catch { }
    }
}

