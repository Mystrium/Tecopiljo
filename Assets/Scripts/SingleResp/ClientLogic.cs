using UnityEngine;
using System;

public class ClientLogic {
    private readonly RectTransform movableRect;
    private readonly Action<byte[]> sendRequestAction;
    private MapManager mapRenderer;

    public ClientLogic(RectTransform movableRect, Action<byte[]> sendRequestAction, MapManager mapper) {
        mapRenderer = mapper;
        this.movableRect = movableRect;
        this.sendRequestAction = sendRequestAction;
    }


    // main TX RX funcs
    public void IntentToMove() { // ---> to server
        // some simple logic

        var pos = ScreenSize();

        byte[] coordBytes = CoordPayload.Pack(new CoordPayload { x = pos[0], y = pos[1] });
        byte[] msg = NetMsg.Pack(NetType.Size, coordBytes);

        sendRequestAction?.Invoke(msg);
    }

    public void RequestMap(int w, int h) {
        byte[] coordBytes = MapInitPayload.Pack(new MapInitPayload { w = w, h = h });
        byte[] msg = NetMsg.Pack(NetType.Map, coordBytes);

        sendRequestAction?.Invoke(msg);
    }

    public void ProcessResponse(byte[] data) { // <--- from server
        // some UI logic
        try {
            NetType type = NetMsg.GetType(data);

            switch (type) {
                case NetType.Map:
                    var mapIn = MapPayload.Unpack(NetMsg.GetPayload(data));

                    Debug.Log("[Client] server resieved map");
                    mapRenderer.RenderMap(mapIn);
                    break;

                case NetType.Move:
                    var pos = CoordPayload.Unpack(NetMsg.GetPayload(data));
                    movableRect.anchoredPosition = new Vector2(pos.x, pos.y);
                    break;

                default:
                    Debug.LogWarning($"[Client] Server wont to: {type}");
                    break;
            }
        } catch { }
    }


    // --- Helpers ---
    private int[] ScreenSize() {
        int halfW = Screen.width / 2;
        int halfH = Screen.height / 2;
        return new int[2] { halfW, halfH };
    }
}
