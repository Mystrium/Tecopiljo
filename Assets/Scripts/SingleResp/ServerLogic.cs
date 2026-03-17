using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class ServerLogic  {
    private readonly Action<byte[]> broadcastAction;

    private LocalMap Map;

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

                    var msg = RandPositionMsg(load);

                    broadcastAction.Invoke(msg); // ---> to clients
                    break;

                case NetType.Map:
                    var mapIn = MapInitPayload.Unpack(NetMsg.GetPayload(data));

                    Map = new LocalMap(mapIn.w, mapIn.h);

                    Map.GenerateMap(mapIn.maxInd);

                    Debug.Log("[Server] Clien wont map: {" + mapIn.w + ":" + mapIn.h + "} tiles = " + mapIn.maxInd);
                    
                    var mapPayload = new MapPayload { w = mapIn.w, h = mapIn.h, mapBytes = Map.CurrentMap };
                    byte[] txBytes = MapPayload.Pack(mapPayload);
                    byte[] txFinal = NetMsg.Pack(NetType.Map, txBytes);

                    broadcastAction.Invoke(txFinal);
                    break;

                case NetType.Move:
                    Debug.Log("[Server] Clien has old APK");
                    break;

                default:
                    Debug.LogWarning($"[Server] Clien wont to: {type}");
                    break;
            }
        } catch (Exception ex) {
            Debug.LogError($"[Server] Помилка обробки: {ex.Message}");
        }
    }


    // --- Helpers ---
    byte[] RandPositionMsg(CoordPayload message) {
        int x = Random.Range(-message.x + 50, message.x - 50);
        int y = Random.Range(-message.y + 50, message.y - 50);

        byte[] moveBytes = CoordPayload.Pack(new CoordPayload { x = x, y = y });

        return NetMsg.Pack(NetType.Move, moveBytes);
    }
}