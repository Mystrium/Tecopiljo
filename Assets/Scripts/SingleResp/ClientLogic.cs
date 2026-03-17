using UnityEngine;
using System;

public class ClientLogic {
    private readonly RectTransform movableRect;
    private readonly Action<byte[]> sendRequestAction;

    public ClientLogic(RectTransform movableRect, Action<byte[]> sendRequestAction) {
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

    public void ProcessResponse(byte[] data) { // <--- from server
        // some UI logic
        try {
            NetType type = NetMsg.GetType(data);

            if (type == NetType.Move) {
                var pos = CoordPayload.Unpack(NetMsg.GetPayload(data));
;
                movableRect.anchoredPosition = new Vector2(pos.x, pos.y);
            }
        } 
        catch { }
    }


    // --- Helpers ---
    private int[] ScreenSize() {
        int halfW = Screen.width / 2;
        int halfH = Screen.height / 2;
        return new int[2] { halfW, halfH };
    }
}