using UnityEngine;
using Random = UnityEngine.Random;
using System;
using System.Collections;

public class ServerLogic  {
    private LocalMap map;

    // TODO: This probably should be in MapInitPayload
    public readonly int[] landTypeCoef = new int[(int)TileLandType.MAX]{
        1, // Water
        5, // Hill
        2, // Desert
    };

    public NetMsg ProcessRequest(
            ServerConnectionMsg cmsg,
            IEnumerable playerIds) {
        NetMsg result = new NetMsg();
        result.header.type = NetType.Error;
        result.payload = new byte[0]; // This is stupid.

        try {
            switch (cmsg.msg.header.type) {
                // case NetType.Size:
                //     var load = CoordPayload.Unpack(NetMsg.GetPayload(data));
                //
                //     if (load.x > 2000 || load.x < -2000) {
                //         Debug.LogWarning($"[Server] Чітерство від {clientId}! Блокуємо рух.");
                //         return;
                //     }
                //
                //     var cmsg.msg = RandPositionMsg(load);
                //
                //     broadcastAction.Invoke(cmsg.msg); // ---> to clients
                //     break;

                case NetType.MapInit:
                    var mapInit = MapInitPayload.Unpack(cmsg.msg.payload);

                    map = new LocalMap(mapInit.w, mapInit.h);
                    MapGenerator.Generate(map, landTypeCoef, playerIds);

                    Debug.Log("[Server] Generating map: (" + mapInit.w + ":" + mapInit.h + ")");

                    var mapPayload = new MapPayload { map = map };
                    result.header.type = NetType.Map;
                    result.payload = MapPayload.Pack(mapPayload);
                    break;

                default:
                    Debug.LogWarning($"[Server] Unknown message type: {cmsg.msg.header.type}");
                    break;
            }
        } catch (Exception ex) {
            Debug.LogError($"[Server] Помилка обробки: {ex.Message}");
        }

        result.header.size = (uint)result.payload.Length;
        return result;
    }
}
