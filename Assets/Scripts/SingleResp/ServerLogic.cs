using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class ServerLogic  {
    private readonly Action<byte[]> broadcastAction;

    public ServerLogic(Action<byte[]> broadcastAction) {
        this.broadcastAction = broadcastAction;
    }

    public void ProcessRequest(int clientId, byte[] data) { // <--- from client
        try {

            NetType type = NetMsg.GetType(data);

            switch (type) {
                case NetType.Size:
                    var load = CoordPayload.Unpack(NetMsg.GetPayload(data));

                    if (load.x > 2000 || load.x < -2000) {
                        Debug.LogWarning($"[Server] Чітерство від {clientId}! Блокуємо рух.");
                        return;
                    }

                    var msg = RandPosotionMsg(load);

                    broadcastAction.Invoke(msg); // ---> to clients
                    break;

                case NetType.Move:
                    Debug.LogError("[Server] Clien has old APK");
                    break;

                default:
                    Debug.LogError($"[Server] Clien wont to: {type}");
                    break;
            }
        } catch (Exception ex) {
            Debug.LogError($"[Server] Помилка обробки: {ex.Message}");
        }
    }


    // --- Helpers ---
    byte[] RandPosotionMsg(CoordPayload message) {
        int x = Random.Range(-message.x + 50, message.x - 50);
        int y = Random.Range(-message.y + 50, message.y - 50);

        byte[] moveBytes = CoordPayload.Pack(new CoordPayload { x = x, y = y });

        return NetMsg.Pack(NetType.Move, moveBytes);
    }
}