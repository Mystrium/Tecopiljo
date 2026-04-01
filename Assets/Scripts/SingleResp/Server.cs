using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using UnityEngine;
using System.Net;
using System;

public struct ServerConnection {
    public int id;
    public TcpClient client;
    public NetworkStream stream;
    public Task recvTask;

    public CancellationTokenSource cts;
};

public struct ServerConnectionMsg {
    public int connId;
    public NetMsg msg;
};

public class Server : IDisposable{
    private ServerLogic logic;

    private CancellationTokenSource serverCTS;

    private TcpListener listener;
    private Task listenerTask;

    // ID -> connection
    private ConcurrentDictionary<int, ServerConnection> connDict;
    private int nextConnId = 0;
    // Here the first task will be listener,
    // other tasks will be stored for reading from connections
    private ConcurrentQueue<ServerConnectionMsg> recvQueue;
    private AutoResetEvent recvSignal;

    private Task procTask;

    private int recvExactly(ServerConnection conn, byte[] buffer) {
        int alreadyRead = 0;
        int length = buffer.Length;

        while (alreadyRead < length) {
            int r = conn.stream.Read(buffer, alreadyRead, length - alreadyRead);
            if (r == 0) {
                return 0;
            }

            alreadyRead += r;
            Debug.Log($"[ServerConnection] Read {alreadyRead} / {length}");
        }

        return alreadyRead;
    }

    // Probably should implement a timeout with a heartbeat for connections...
    public void recvLoop(ServerConnection conn, CancellationToken token) {
        // Hopefully this actually gives the correct size
        int headerSize = Marshal.SizeOf(typeof(NetMsgHeader));

        try {
            while (!token.IsCancellationRequested) {
                byte[] headBuf = new byte[headerSize];

                int read = recvExactly(conn, headBuf);
                if (read == 0) {
                    Debug.LogError($"[ServerConnection] Failed to receive exactly {headerSize} bytes for the header");
                    break;
                }

                // Debug.Log($"[Server] Received header: {BitConverter.ToString(headBuf)}");
                NetMsgHeader head = NetMsgHeader.Unpack(headBuf);
                byte[] msgBuf = new byte[head.size];

                read = recvExactly(conn, msgBuf);
                if (read == 0) {
                    Debug.LogError($"[ServerConnection] Failed to receive exactly {headerSize} bytes for the message");
                    continue;
                }

                NetMsg nmsg = new NetMsg {
                    header = head,
                    payload = msgBuf,
                };

                ServerConnectionMsg cmsg = new ServerConnectionMsg {
                    connId = conn.id,
                    msg = nmsg,
                };

                Debug.Log($"[ServerConnection] Received message: connId = {cmsg.connId}, type = {cmsg.msg.header.type}");
                recvQueue.Enqueue(cmsg);
                recvSignal.Set();
            }
        } catch (OperationCanceledException) { }

        Debug.Log($"[ServerConnection] Stopped receiving for client {conn.id}");
    }

    public void broadcastMsg(NetMsg msg) {
        Debug.Log($"[Server] Broadcasting message with type = {msg.header.type}");
        foreach (var conn in connDict.Values) {
            sendMsg(conn, msg);
        }
    }

    public void sendMsg(ServerConnection conn, NetMsg msg) {
        msg.header.size = (uint)msg.payload.Length;
        conn.stream.Write(NetMsgHeader.Pack(msg.header));
        conn.stream.Write(msg.payload);
    }

    public void processMsg(ServerConnectionMsg cmsg) {
        Debug.Log($"[Server] Processing message: connId = {cmsg.connId}, type = {cmsg.msg.header.type}");
        // Probably here I also need to sometimes send data only to responding client.
        broadcastMsg(logic.ProcessRequest(cmsg, connDict.Keys));
    }

    public void processLoop(CancellationToken token) {
        while (!token.IsCancellationRequested) {
            recvSignal.WaitOne();
            while (recvQueue.TryDequeue(out ServerConnectionMsg msg)) {
                MainDispatcher.Enqueue(() => processMsg(msg));
            }
        }
    }

    public void printConnections() {
        foreach(var entry in connDict) {
            Debug.Log($"Active client id = {entry.Key}");
        }
    }

    public void startConnection(TcpClient client) {
        int connId = nextConnId++;

        var newConn = new ServerConnection();
        newConn.id = connId;
        newConn.client = client;
        newConn.stream = client.GetStream();
        newConn.cts = new CancellationTokenSource();
        newConn.recvTask = Task.Run(() => recvLoop(newConn, newConn.cts.Token));

        connDict.TryAdd(connId, newConn);
        Debug.Log($"[Server] New client connected with ID {connId}");
    }

    private void acceptLoop(CancellationToken token) {
        try {
            while (!token.IsCancellationRequested) {
                var client = listener.AcceptTcpClient();
                startConnection(client);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) {
            Debug.LogError("[Server] AcceptLoop loop error: " + ex.Message);
        }
    }

    public void start(int port) {
        serverCTS = new CancellationTokenSource();
        listener = new TcpListener(IPAddress.Any, port);
        connDict = new ConcurrentDictionary<int, ServerConnection>();
        recvQueue = new ConcurrentQueue<ServerConnectionMsg>();
        recvSignal = new AutoResetEvent(false);
        logic = new ServerLogic(); // TODO: Remove this

        listener.Server.SetSocketOption(
            SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        listener.Start();

        listenerTask = Task.Run(() => acceptLoop(serverCTS.Token));
        procTask = Task.Run(() => processLoop(serverCTS.Token));
    }

    public void stopConnection(ServerConnection conn) {
        conn.cts.Cancel();
        conn.stream.Close();
        conn.client.Close();

        Debug.Log($"[Server] Stopped connection with id {conn.id}");
    }

    public void stop() {
        serverCTS.Cancel();
        listener.Stop();

        foreach (var conn in connDict.Values) {
            stopConnection(conn);
        }
        connDict.Clear();
    }

    public void Dispose() {
        stop();
    }
}
