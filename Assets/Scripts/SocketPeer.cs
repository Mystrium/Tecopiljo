using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using UnityEngine;
using System.Net;
using System;

/// <summary>
/// Simple TCP peer that can act as Host or Client.
/// - Length-prefixed messages (4 bytes network order)
/// - Events are invoked on main thread via UnityMainThreadDispatcher
/// Usage:
///   var peer = new SimpleSocketPeer();
///   peer.OnMessage += (msg) => { /* handle on main thread */ };
///   await peer.StartHostAsync(7777);
///   await peer.SendAsync("hello");
///   await peer.CloseAsync();
/// </summary>
public class SocketPeer : IDisposable {
    TcpListener listener;
    TcpClient client;
    NetworkStream stream;
    CancellationTokenSource cts;
    Task receiveLoopTask;
    Task sendLoopTask;
    readonly ConcurrentQueue<string> sendQueue = new ConcurrentQueue<string>();
    readonly SemaphoreSlim sendSignal = new SemaphoreSlim(0);

    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string> OnMessage;
    public bool IsConnected => client != null && client.Connected;

    // HEARTBEAT optional
    public bool EnableHeartbeat = true;
    public int HeartbeatIntervalSec = 5;

    // === HOST ===
    public async Task StartHostAsync(int port) {
        await Task.Run(() => {
            cts = new CancellationTokenSource();
            // create listener
            listener = new TcpListener(IPAddress.Any, port);

            // allow quick reuse on many platforms
            try {
                listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            }
            catch { /* ignore if platform doesn't allow */ }

            listener.Start();

            Debug.Log($"[SimpleSocketPeer] Listening on port {port}");
            // Accept one client (blocking) - AcceptTcpClientAsync will be cancelled when listener.Stop() is called
            try {
                var acceptTask = listener.AcceptTcpClientAsync();
                acceptTask.Wait(cts.Token); // block here but cancellable
                client = acceptTask.Result;
                try { client.NoDelay = true; } catch { }
            } catch (OperationCanceledException) { return; }
            catch (Exception ex) {
                Debug.LogWarning("[SimpleSocketPeer] Host accept error: " + ex.Message);
                CloseInternal(); 
                return;
            }

            // set up stream and start loops
            stream = client.GetStream();
            StartLoops();
            // notify main thread
            UnityMainThreadDispatcher.Enqueue(()=> OnConnected?.Invoke());
        });
    }

    // === CLIENT ===
    public async Task StartClientAsync(string host, int port, int connectTimeoutMs = 8000) {
        cts = new CancellationTokenSource();
        client = new TcpClient();
        var connectTask = client.ConnectAsync(host, port);
        var timeoutTask = Task.Delay(connectTimeoutMs, cts.Token);
        var completed = await Task.WhenAny(connectTask, timeoutTask);
        if (completed != connectTask) {
            // timeout
            CloseInternal();
            UnityMainThreadDispatcher.Enqueue(()=> Debug.LogWarning("[SimpleSocketPeer] Connect timeout"));
            return;
        }

        stream = client.GetStream();
        StartLoops();
        UnityMainThreadDispatcher.Enqueue(()=> OnConnected?.Invoke());
    }

    // start send/receive loops
    void StartLoops() {
        // receive loop
        receiveLoopTask = Task.Run(async () => {
            try {
                var lenBuf = new byte[4];
                while (!cts.IsCancellationRequested && client != null && client.Connected) {
                    // read length
                    int read = await ReadExactlyAsync(lenBuf, 0, 4, cts.Token);
                    if (read == 0) break;
                    int netlen = BitConverter.ToInt32(lenBuf, 0);
                    int len = IPAddress.NetworkToHostOrder(netlen);
                    if (len <= 0) continue;
                    var buf = new byte[len];
                    read = await ReadExactlyAsync(buf, 0, len, cts.Token);
                    if (read == 0) break;
                    string msg = Encoding.UTF8.GetString(buf);
                    UnityMainThreadDispatcher.Enqueue(()=> OnMessage?.Invoke(msg));
                }
            }
            catch (OperationCanceledException) { /* canceled */ }
            catch (Exception ex) { Debug.LogWarning("[SimpleSocketPeer] ReceiveLoop error: " + ex.Message); }
            finally {
                // connection closed
                UnityMainThreadDispatcher.Enqueue(()=> OnDisconnected?.Invoke());
                CloseInternal();
            }
        }, cts.Token);

        sendLoopTask = Task.Run(async () => {
            try {
                while (!cts.IsCancellationRequested && client != null && client.Connected) {
                    // чекаємо сигнал (без таймауту або з невеликим таймаутом)
                    await sendSignal.WaitAsync(1000, cts.Token); // додатково таймаут 1s, щоб перевіряти heartbeat/закриття
                    // після пробудження відправляємо всі наявні повідомлення
                    while (sendQueue.TryDequeue(out var msg)) {
                        var bytes = Encoding.UTF8.GetBytes(msg);
                        int netlen = IPAddress.HostToNetworkOrder(bytes.Length);
                        var lenBytes = BitConverter.GetBytes(netlen);
                        try {
                            await stream.WriteAsync(lenBytes, 0, 4, cts.Token);
                            await stream.WriteAsync(bytes, 0, bytes.Length, cts.Token);
                            await stream.FlushAsync(cts.Token);
                        }
                        catch (OperationCanceledException) { break; }
                    }
                }
            }
            catch (OperationCanceledException) { /* cancelled */ }
            catch (Exception ex) { Debug.LogWarning("[SimpleSocketPeer] SendLoop error: " + ex.Message); }
        }, cts.Token);
    }

    // async read exactly
    async Task<int> ReadExactlyAsync(byte[] buffer, int offset, int count, CancellationToken token) {
        int total = 0;
        while (total < count) {
            int r = await stream.ReadAsync(buffer, offset + total, count - total, token);
            if (r == 0) return 0;
            total += r;
        }
        return total;
    }

    public void Send(string message) {
        if (string.IsNullOrEmpty(message)) return;
        sendQueue.Enqueue(message);
        // одразу сигналимо send-loop, щоб він не сидів у Task.Delay
        try { sendSignal.Release(); } catch { /* ignore if disposed */ }
    }

    // close everything gracefully
    public async Task CloseAsync() {
        if (cts != null && !cts.IsCancellationRequested) cts.Cancel();

        try {
            // stop receiving/accepting
            if (listener != null) {
                try { listener.Stop(); } catch {}
            }

            if (client != null) {
                try { client.Client.Shutdown(SocketShutdown.Both); } catch {}
                try { client.Close(); } catch {}
            }

            // wait small time for loops to end
            var tasks = new System.Collections.Generic.List<Task>();
            if (receiveLoopTask != null) tasks.Add(receiveLoopTask);
            if (sendLoopTask != null) tasks.Add(sendLoopTask);
            if (tasks.Count > 0) await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(1000));
        }
        catch (Exception ex) { Debug.LogWarning("[SimpleSocketPeer] CloseAsync error: " + ex.Message); }
        finally { Dispose(); }
    }

    void CloseInternal() {
        try { cts?.Cancel(); } catch {}
        try { stream?.Close(); } catch {}
        try { client?.Close(); } catch {}
        try { listener?.Stop(); } catch {}
        stream = null; client = null; listener = null;
    }

    public void Dispose() {
        CloseInternal();
        try { cts?.Dispose(); } catch {}
        cts = null;
    }
}