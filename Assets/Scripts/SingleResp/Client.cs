using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using System;
using Unity.Mathematics;
using System.Data.Common;

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
    private int playerId = -1;
    private MapManager mapRenderer;

    public Client(MapManager mapper) {
        mapRenderer = mapper;
        mapRenderer.OnMoveIntent += RequestMoveUnit;
    }

    public void RequestMap(int w, int h) {
        NetMsg msg = new NetMsg {
            payload = MapInitPayload.Pack(new MapInitPayload { w = w, h = h })
        };
        msg.header = new NetMsgHeader {
            type = NetType.MapInit,
            size = (uint)msg.payload.Length,
        };

        Debug.Log($"[Client] size = {msg.header.size}, type = {msg.header.type}");

        ClientSend(NetMsg.Pack(msg));
    }

    private void RequestUnit(int2 pos, UnitType type) {
        Debug.Log("request innit unit");
        NetMsg msg = new NetMsg {
            payload = UnitPayload.Pack(new UnitPayload { type = type, x = pos.x, y = pos.y, playerIdx = playerId })
        };
        msg.header = new NetMsgHeader {
            type = NetType.InnitUnit,
            size = (uint)msg.payload.Length,
        };

        Debug.Log($"[Client] size = {msg.header.size}, type = {msg.header.type}");

        ClientSend(NetMsg.Pack(msg));
    }

    private void RequestMoveUnit(int unitId, Vector2Int targetPos, int player) {
        if(player != playerId) return; // yea, stupid

        NetMsg msg = new NetMsg {
            payload = MoveUnitPayload.Pack(new MoveUnitPayload { 
                unitId = unitId, 
                x = targetPos.x, 
                y = targetPos.y 
            })
        };
        msg.header = new NetMsgHeader {
            type = NetType.MoveUnit,
            size = (uint)msg.payload.Length,
        };

        Debug.Log($"[Client] Sending move intent for unit {unitId} to [{targetPos.x}, {targetPos.y}]");
        ClientSend(NetMsg.Pack(msg));
    }

    public void ProcessResponse(NetMsg msg) { // <--- from server
        // some UI logic
        try {
            switch (msg.header.type) {
                case NetType.PlayerJoined:
                    if(playerId == -1)
                        playerId = PlayerJoinedPayload.Unpack(msg.payload).playerId;
                    break;

                case NetType.Map:
                    var mapIn = MapPayload.Unpack(msg.payload);

                    Debug.Log("[Client] Received map");
                    mapRenderer.RenderMap(mapIn);

                    // or some another action to trigger first worker spawn
                    InnitWorker(mapIn.map);
                    break;

                case NetType.SpawnUnit:
                    var unitIn = NetUnit.Unpack(msg.payload);
                    Debug.Log("[Client] Received unit");

                    mapRenderer.RenderUnit(unitIn);
                    break;

                case NetType.MoveUnit:
                    var moveData = MoveUnitPayload.Unpack(msg.payload);
                    Debug.Log($"[Client] Server approved move for unit {moveData.unitId}");
                    
                    mapRenderer.MoveUnitVisual(moveData.unitId, moveData.x, moveData.y);
                    break;

                case NetType.Error:
                    var errData = ErrorPayload.Unpack(msg.payload);
                    Debug.LogWarning($"[Server Error] {errData.message}");
                    break;

                default:
                    Debug.LogWarning($"[Client] Unknown server message type: {msg.header.type}");
                    break;
            }
        } catch { }
    }

    private void InnitWorker(LocalMap map) {
        int2 sec = getMapSector();

        int sectorWidth = map.w / 3;
        int sectorHeight = map.h / 3;

        int minX = sec.x * sectorWidth;
        int maxX = (sec.x + 1) * sectorWidth;

        int minY = sec.y * sectorHeight;
        int maxY = (sec.y + 1) * sectorHeight;

        int2 pos = new int2(
            UnityEngine.Random.Range(minX, maxX),
            UnityEngine.Random.Range(minY, maxY)
        );

        RequestUnit(pos, UnitType.Worker);
    }

    private int2 getMapSector() {
        switch (playerId) {
            case 0: return new int2(0,0);
            case 1: return new int2(2,2);
            case 2: return new int2(0,2);
            case 3: return new int2(2,0);
            case 4: return new int2(0,1);
            case 5: return new int2(2,1);
            case 6: return new int2(1,0);
            case 7: return new int2(1,2);
            default: return new int2(1,1);
        }
    }
}

