using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;

public class NetworkUIController : MonoBehaviour {
    public InputField ipInput;
    public InputField portInput;
    public Button hostBtn;
    public Button connectBtn;
    public Button moveBtn;
    public Text statusText;
    public RectTransform movableRect;

    SocketPeer peer;

    void Start() {
        hostBtn.onClick.AddListener(()=> _ = StartHost());
        connectBtn.onClick.AddListener(()=> _ = StartClient());
        moveBtn.onClick.AddListener(OnLocalMove);

        if (string.IsNullOrEmpty(portInput.text)) portInput.text = "7777";
    }

    async Task StartHost() {
        SetButtonsInteractable(false);
        peer = new SocketPeer();
        peer.OnConnected += () => { statusText.text = "Connected (host)"; SetButtonsInteractable(true); };
        peer.OnDisconnected += () => { statusText.text = "Disconnected"; SetButtonsInteractable(false); };
        peer.OnMessage += (msg) => HandleIncoming(msg);

        statusText.text = "Starting host...";
        await peer.StartHostAsync(int.Parse(portInput.text));
    }

    async Task StartClient() {
        SetButtonsInteractable(false);
        peer = new SocketPeer();
        peer.OnConnected += () => { statusText.text = "Connected (client)"; SetButtonsInteractable(true); };
        peer.OnDisconnected += () => { statusText.text = "Disconnected"; SetButtonsInteractable(false); };
        peer.OnMessage += (msg) => HandleIncoming(msg);

        statusText.text = "Connecting...";
        await peer.StartClientAsync(ipInput.text.Trim(), int.Parse(portInput.text));
    }

    void HandleIncoming(string json) {
        // тут парсим json і оновлюємо UI (ми вже в main thread через dispatcher)
        statusText.text = "Msg: " + json;
        // якщо це move-режим — розпарсимо і пересунемо movableRect
        try {
            var m = JsonUtility.FromJson<NetMsg>(json);
            if (m.type == "move") movableRect.anchoredPosition = new Vector2(m.x, m.y);
        }
        catch { }
    }

    void OnLocalMove() {
        var pos = RandomUINearScreen();
        // movableRect.anchoredPosition = pos;
        var msg = JsonUtility.ToJson(new NetMsg { type="move", x=pos.x, y=pos.y });
        peer?.Send(msg);
    }

    Vector2 RandomUINearScreen() {
        float halfW = Screen.width / 2f;
        float halfH = Screen.height / 2f;
        float x = Random.Range(-halfW + 50f, halfW - 50f);
        float y = Random.Range(-halfH + 50f, halfH - 50f);
        return new Vector2(x,y);
    }

    public async void OnQuitPressed() {
        // викликається з UI — коректне завершення і вихід
        SetButtonsInteractable(false);
        statusText.text = "Closing...";
        if (peer != null) await peer.CloseAsync();
        Application.Quit();
    }

    void SetButtonsInteractable(bool state) {
        UnityMainThreadDispatcher.Enqueue(() => {
            hostBtn.interactable = state;
            connectBtn.interactable = state;
            moveBtn.interactable = state;
        });
    }

    [System.Serializable]
    public class NetMsg { public string type; public float x; public float y; }
}