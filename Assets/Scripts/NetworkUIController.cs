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
    public Text statusText;

    // test
    public Button moveBtn;
    public RectTransform movableRect;

    // panels
    public GameObject mainMenuPanel;
    public GameObject multiplayerPanel;
    public GameObject gamePanel;

    public Button singlePlayer;
    public Button multiPlayer;

    public Button startPlay;

    // peers
    SocketPeer serverPeer;
    SocketPeer clientPeer;

    // Server.host > Client.connect > Client.ClientSend > Server.OnRequest > Logic > Server.ServerBroadcast > Client.OnResponse


    void Start() {
        hostBtn.onClick.AddListener(()=> _ = StartHost());
        connectBtn.onClick.AddListener(()=> _ = StartClient());
        moveBtn.onClick.AddListener(OnLocalMove);
        startPlay.onClick.AddListener(ShowGame);

        singlePlayer.onClick.AddListener(()=> _ = StartHost());
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


    async Task StartHost() {
        SetButtonsInteractable(false);
        statusText.text = "Starting host on " + GetLocalIPAddress() + ":" + portInput.text;

        serverPeer = new SocketPeer();

        serverPeer.OnHosted += () => _ = StartLocalClient();

        serverPeer.OnServerClientConnected += (clientId) => {
            Debug.Log($"[Server] Player {clientId} connected.");
        };
        serverPeer.OnServerClientDisconnected += (clientId) => {
            Debug.Log($"[Server] Player {clientId} disconected.");
        };
        
        serverPeer.OnRequest += (clientId, msg) => {
            serverPeer.ServerBroadcast(msg); 
        };

        int port = int.Parse(portInput.text);
        await serverPeer.StartHostAsync(port);
    }

    async Task StartLocalClient() {
        clientPeer = new SocketPeer();
        int port = int.Parse(portInput.text);

        clientPeer.OnClientConnected += () => { 
            statusText.text = GetLocalIPAddress() + ':' + port; 
            SetButtonsInteractable(true);
        };
        clientPeer.OnClientDisconnected += () => { 
            statusText.text = "Server stopped"; 
            SetButtonsInteractable(true); 
            ShowMainMenu(); 
        };

        clientPeer.OnResponse += (msg) => HandleIncoming(msg);

        await clientPeer.StartClientAsync("127.0.0.1", port);
    }

    async Task StartClient() {
        SetButtonsInteractable(false);
        statusText.text = "Connecting...";

        clientPeer = new SocketPeer();

        clientPeer.OnClientConnected += () => { 
            statusText.text = "Connected to host"; 
            SetButtonsInteractable(true); 
            ShowGame(); 
        };
        clientPeer.OnClientDisconnected += () => { 
            statusText.text = "Host stopped / Disconnected"; 
            SetButtonsInteractable(true); 
            ShowMainMenu(); 
        };

        clientPeer.OnResponse += (msg) => HandleIncoming(msg);

        string ip = ipInput.text.Trim();
        int port = int.Parse(portInput.text);
        await clientPeer.StartClientAsync(ip, port);
    }

    void HandleIncoming(string json)  {
        statusText.text = "Msg: " + json;
        try  {
            var m = JsonUtility.FromJson<NetMsg>(json);
            
            if (m.type == "move")  {
                // Оскільки гравців тепер багато, в NetMsg добре було б додати поле ID гравця,
                // щоб знати, який саме Rect рухати.
                movableRect.anchoredPosition = new Vector2(m.x, m.y);
            }
        } catch { }
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
        var msg = JsonUtility.ToJson(new NetMsg { type = "move", x = pos[0], y = pos[1] });

        if (clientPeer != null)
            clientPeer.ClientSend(msg);
    }

    int[] RandomUINearScreen() {
        int halfW = Screen.width / 2;
        int halfH = Screen.height / 2;
        int x = Random.Range(-halfW + 50, halfW - 50);
        int y = Random.Range(-halfH + 50, halfH - 50);
        return new int[2] {x, y};
    }

    public void OnQuitPressed() {
        if(mainMenuPanel.activeSelf) {
            Debug.Log("bye");
            Application.Quit();
        } else {
            statusText.text = "Closing...";
            if (serverPeer != null)
                serverPeer.CloseAll();

            if (clientPeer != null)
                clientPeer.CloseAll();
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