using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class ServerLogic  {
    private readonly Action<string> broadcastAction;

    public ServerLogic(Action<string> broadcastAction) {
        this.broadcastAction = broadcastAction;
    }

    public void ProcessRequest(int clientId, string json) { // <--- from client
        try {
            var m = JsonUtility.FromJson<NetMsg>(json);

            switch (m.type) {
                case "size":
                    if (m.x > 2000 || m.x < -2000) {
                        Debug.LogWarning($"[Server] Чітерство від {clientId}! Блокуємо рух.");
                        return;
                    }

                    var msg = RandPosotionMsg(m);

                    broadcastAction.Invoke(msg); // ---> to clients
                    break;

                case "move":
                    Debug.LogError("[Server] Clien has old APK");
                    break;

                default:
                    Debug.LogError($"[Server] Clien wont to: {m.type}");
                    break;
            }
        } catch (Exception ex) {
            Debug.LogError($"[Server] Помилка обробки: {ex.Message}");
        }
    }


    // --- Helpers ---
    string RandPosotionMsg(NetMsg message) {
        int x = Random.Range(-message.x + 50, message.x - 50);
        int y = Random.Range(-message.y + 50, message.y - 50);

        return JsonUtility.ToJson(new NetMsg { type = "move", x = x, y = y });
    }
}