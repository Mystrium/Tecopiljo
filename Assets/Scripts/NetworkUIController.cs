using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;
using System.Net;
using System.Net.Sockets;

public class NetworkUIController : MonoBehaviour {
    [Header("Network UI")]
    public InputField ipInput;
    public InputField portInput;
    public Button hostBtn;
    public Button connectBtn;
    public Text statusText;

    [Header("Game UI")]
    public Button moveBtn;
    public RectTransform movableRect;
    public Button startPlay;

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject multiplayerPanel;
    public GameObject gamePanel;
    public Button singlePlayer;
    public Button multiPlayer;

    // TCP Sockets
    private SocketPeer serverPeer;
    private SocketPeer clientPeer;

    // Backend logic
    private ServerLogic serverLogic;
    private ClientLogic clientLogic;

    void Start() {
        hostBtn.onClick.AddListener(() => _ = StartHost());
        connectBtn.onClick.AddListener(() => _ = StartClient());
        startPlay.onClick.AddListener(ShowGame);

        singlePlayer.onClick.AddListener(() => _ = StartHost()); // illusion
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

    public void ShowMainMenu() {    mainMenuPanel.SetActive(true);  multiplayerPanel.SetActive(false); gamePanel.SetActive(false); }
    public void ShowMultiplayer() { mainMenuPanel.SetActive(false); multiplayerPanel.SetActive(true);  gamePanel.SetActive(false); }
    public void ShowGame() {        mainMenuPanel.SetActive(false); multiplayerPanel.SetActive(false); gamePanel.SetActive(true);  }

    async Task StartHost() {
        SetButtonsInteractable(false);
        statusText.text = "Starting host on " + GetLocalIPAddress() + ":" + portInput.text;

        serverPeer = new SocketPeer();

        serverLogic = new ServerLogic(serverPeer.ServerBroadcast);

        serverPeer.OnHosted += () => _ = StartLocalClient();
        serverPeer.OnServerClientConnected += (clientId) => Debug.Log($"[Server] Player {clientId} connected.");
        serverPeer.OnServerClientDisconnected += (clientId) => Debug.Log($"[Server] Player {clientId} disconected.");

        serverPeer.OnRequest += serverLogic.ProcessRequest;

        int port = int.Parse(portInput.text);
        await serverPeer.StartHostAsync(port);
    }

    async Task StartLocalClient() {
        clientPeer = new SocketPeer();
        int port = int.Parse(portInput.text);

        SetupClientLogic();

        clientPeer.OnClientConnected += () => { 
            statusText.text = GetLocalIPAddress() + ':' + port; 
            SetButtonsInteractable(true);
        };
        clientPeer.OnClientDisconnected += () => { 
            statusText.text = "Server stopped"; 
            SetButtonsInteractable(true); 
            ShowMainMenu(); 
        };

        await clientPeer.StartClientAsync("127.0.0.1", port);
    }

    async Task StartClient() {
        SetButtonsInteractable(false);
        statusText.text = "Connecting...";

        clientPeer = new SocketPeer();

        SetupClientLogic();

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

        string ip = ipInput.text.Trim();
        int port = int.Parse(portInput.text);
        await clientPeer.StartClientAsync(ip, port);
    }

    private void SetupClientLogic() {
        clientLogic = new ClientLogic(movableRect, clientPeer.ClientSend);
        moveBtn.onClick.AddListener(clientLogic.IntentToMove);
        clientPeer.OnResponse += clientLogic.ProcessResponse;
    }


    // --- Helpers ---
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
        MainDispatcher.Enqueue(() => {
            hostBtn.interactable = state;
            connectBtn.interactable = state;
        });
    }
}