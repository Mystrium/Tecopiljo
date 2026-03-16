using UnityEngine;
using System;

public class ClientLogic {
    private readonly RectTransform movableRect;
    private readonly Action<string> sendRequestAction;

    public ClientLogic(RectTransform movableRect, Action<string> sendRequestAction) {
        this.movableRect = movableRect;
        this.sendRequestAction = sendRequestAction;
    }


    // main TX RX funcs
    public void IntentToMove() { // ---> to server
        // some simple logic

        var pos = ScreenSize();
        var msg = JsonUtility.ToJson(new NetMsg { type = "size", x = pos[0], y = pos[1] });

        sendRequestAction?.Invoke(msg);
    }

    public void ProcessResponse(string json) { // <--- from server
        // some UI logic
        try {
            var m = JsonUtility.FromJson<NetMsg>(json);
            
            if (m.type == "move") {
                movableRect.anchoredPosition = new Vector2(m.x, m.y);
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