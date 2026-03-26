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

    [Header("Innit UI")]
    public InputField widthInput;
    public InputField heightInput;
    public Button innitGame;

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject multiplayerPanel;
    public GameObject gamePanel;
    public GameObject innitPanel;

    public Button singlePlayer;
    public Button multiPlayer;

    [Header("Other")]
    public GameObject Mapper;

    // TCP Sockets
    private Server server;
    private Client client;

    // Backend logic
    private ServerLogic serverLogic;
    private MapManager MapScript;

    void Start() {
        MapScript = Mapper.GetComponent<MapManager>();

        hostBtn.onClick.AddListener(() => StartHost());
        connectBtn.onClick.AddListener(() => _ = StartClient());
        startPlay.onClick.AddListener(StartGame);
        innitGame.onClick.AddListener(ShowInnit);

        singlePlayer.onClick.AddListener(() => StartHost()); // illusion
        multiPlayer.onClick.AddListener(ShowMultiplayer);

        ShowMainMenu();
        if (string.IsNullOrEmpty(portInput.text)) portInput.text = "7777";
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            OnQuitPressed();
            ShowMainMenu();
        }

        MapScript.GetClickedTile();
    }

    public void ShowMainMenu() {    mainMenuPanel.SetActive(true);  multiplayerPanel.SetActive(false); gamePanel.SetActive(false); innitPanel.SetActive(false); }
    public void ShowMultiplayer() { mainMenuPanel.SetActive(false); multiplayerPanel.SetActive(true);  gamePanel.SetActive(false); innitPanel.SetActive(false); }
    public void ShowInnit() {       mainMenuPanel.SetActive(false); multiplayerPanel.SetActive(false); gamePanel.SetActive(false); innitPanel.SetActive(true);  }
    public void ShowGame() {        mainMenuPanel.SetActive(false); multiplayerPanel.SetActive(false); gamePanel.SetActive(false);  innitPanel.SetActive(false); }

    void StartGame() {
        int width = int.Parse(widthInput.text);
        int height = int.Parse(heightInput.text);

        client.RequestMap(width, height);
        ShowGame();
    }

    async void StartHost() {
        SetButtonsInteractable(false);
        statusText.text = "Starting host on " + GetLocalIPAddress() + ":" + portInput.text;

        server = new Server();

        int port = int.Parse(portInput.text);
        server.start(port);
        await StartLocalClient();
    }

    async Task StartLocalClient() {
        client = new Client(movableRect, MapScript);
        int port = int.Parse(portInput.text);

        client.OnClientConnected += () => {
            statusText.text = GetLocalIPAddress() + ':' + port;
            SetButtonsInteractable(true);
        };
        client.OnClientDisconnected += () => {
            statusText.text = "Server stopped";
            SetButtonsInteractable(true);
            ShowMainMenu();
        };

        await client.StartClientAsync("127.0.0.1", port);
    }

    async Task StartClient() {
        SetButtonsInteractable(false);
        statusText.text = "Connecting...";

        client = new Client(movableRect, MapScript);

        client.OnClientConnected += () => {
            statusText.text = "Connected to host";
            SetButtonsInteractable(true);
            ShowGame();
        };
        client.OnClientDisconnected += () => {
            statusText.text = "Host stopped / Disconnected";
            SetButtonsInteractable(true);
            ShowMainMenu();
        };

        string ip = ipInput.text.Trim();
        int port = int.Parse(portInput.text);
        await client.StartClientAsync(ip, port);
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
            if (server != null)
                server.Dispose();

            if (client != null)
                client.Dispose();
        }
    }

    void SetButtonsInteractable(bool state) {
        MainDispatcher.Enqueue(() => {
            hostBtn.interactable = state;
            connectBtn.interactable = state;
        });
    }
}
