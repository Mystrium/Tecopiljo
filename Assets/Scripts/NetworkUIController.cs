using System.Threading.Tasks;
using System.Net.Sockets;
using UnityEngine.UI;
using UnityEngine;
using System.Net;

public class NetworkUIController : MonoBehaviour {
    public InputField ipInput;
    public InputField portInput;
    public Button hostBtn;
    public Button connectBtn;
    public Button moveBtn;
    public Text statusText;
    public RectTransform movableRect;

    public GameObject mainMenuPanel;
    public GameObject multiplayerPanel;
    public GameObject gamePanel;

    public Button singlePlayer;
    public Button multiPlayer;

    SocketPeer peer;

    void Start() {
        hostBtn.onClick.AddListener(()=> _ = StartHost());
        connectBtn.onClick.AddListener(()=> _ = StartClient());
        moveBtn.onClick.AddListener(OnLocalMove);

        singlePlayer.onClick.AddListener(()=> _ = ConnectLocaly());
        multiPlayer.onClick.AddListener(ShowMultiplayer);

        ShowMainMenu();

        if (string.IsNullOrEmpty(portInput.text)) portInput.text = "7777";
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            OnQuitPressed();
            ShowMainMenu();
        }
    }

    public void ShowMainMenu() {
        mainMenuPanel.SetActive(true);
        multiplayerPanel.SetActive(false);
        gamePanel.SetActive(false);
    }

    public void ShowMultiplayer() {
        mainMenuPanel.SetActive(false);
        multiplayerPanel.SetActive(true);
        gamePanel.SetActive(false);
    }

    public void ShowGame() {
        mainMenuPanel.SetActive(false);
        multiplayerPanel.SetActive(false);
        gamePanel.SetActive(true);
    }

    async Task ConnectLocaly() {
        peer = new SocketPeer();
        peer.OnHosted += () => _ = LocalPlay();

        await peer.StartHostAsync(7777);
    }

    async Task LocalPlay() {
        SocketPeer clt = new SocketPeer();
        clt.OnConnected += () => { ShowGame(); };
        clt.OnMessage += (msg) => HandleIncoming(msg);

        await clt.StartClientAsync("127.0.0.1", 7777);
    }

    async Task StartHost() {
        SetButtonsInteractable(false);
        peer = new SocketPeer();
        peer.OnConnected += () => { statusText.text = "Client connected"; SetButtonsInteractable(true); ShowGame(); };
        peer.OnDisconnected += () => { statusText.text = "Client disconnected"; SetButtonsInteractable(true); ShowMainMenu(); };
        peer.OnMessage += (msg) => HandleIncoming(msg);

        statusText.text = "Starting host on " + GetLocalIPAddress() + ":" + portInput.text;
        await peer.StartHostAsync(int.Parse(portInput.text));
    }

    async Task StartClient() {
        SetButtonsInteractable(false);
        peer = new SocketPeer();
        peer.OnConnected += () => { statusText.text = "Connected to host"; SetButtonsInteractable(true); ShowGame(); };
        peer.OnDisconnected += () => { statusText.text = "Host stoped"; SetButtonsInteractable(true); ShowMainMenu(); };
        peer.OnMessage += (msg) => HandleIncoming(msg);

        statusText.text = "Connecting...";
        await peer.StartClientAsync(ipInput.text.Trim(), int.Parse(portInput.text));
    }

    void HandleIncoming(string json) { // handle commands
        statusText.text = "Msg: " + json;
        try {
            var m = JsonUtility.FromJson<NetMsg>(json);
            if (m.type == "move") // must be giga switch
                movableRect.anchoredPosition = new Vector2(m.x, m.y);
        }
        catch { }
    }

    public string GetLocalIPAddress() {
        string localIP = "127.0.0.1";
        try {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList) {
                if (ip.AddressFamily == AddressFamily.InterNetwork) {
                    localIP = ip.ToString();
                    break;
                }
            }
        } catch { }
        return localIP;
    }

    void OnLocalMove() {
        var pos = RandomUINearScreen();
        // movableRect.anchoredPosition = pos;
        var msg = JsonUtility.ToJson(new NetMsg { type = "move", x = pos[0], y = pos[1] });
        peer?.Send(msg);
    }

    int[] RandomUINearScreen() {
        int halfW = Screen.width / 2;
        int halfH = Screen.height / 2;
        int x = Random.Range(-halfW + 50, halfW - 50);
        int y = Random.Range(-halfH + 50, halfH - 50);
        return new int[2] {x, y};
    }

    public async void OnQuitPressed() {
        if(mainMenuPanel.activeSelf) {
            Debug.Log("bye");
            Application.Quit();
        } else {
            statusText.text = "Closing...";
            if (peer != null)
                await peer.CloseAsync();
        }
    }

    void SetButtonsInteractable(bool state) {
        UnityMainThreadDispatcher.Enqueue(() => {
            hostBtn.interactable = state;
            connectBtn.interactable = state;
        });
    }

    [System.Serializable]
    public class NetMsg { public string type; public float x; public float y; }
}