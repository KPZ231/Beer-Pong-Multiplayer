using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;

public class Network_UI_Manager : MonoBehaviour
{
    public static Network_UI_Manager instance { get; private set; }

    [Header("UI Elements")]
    public TMP_InputField serverNameInput;
    public TMP_InputField serverCodeInput;
    public TMP_InputField emailInput;
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI lobbyNameText; // Dodany element do wyświetlania nazwy lobby
    public GameObject LobbyUI = null;

    [Header("Player Setup")]
    public GameObject playerPrefab;  // Prefab dla gracza
    public Transform gridTransform;  // Transform gridu
    public string lobbyName = String.Empty;
    public bool isPrivateSession = true; // Zmienna do określania rodzaju sesji
    private const int maxPlayers = 5; // Maksymalna liczba graczy
    private string serverCode;

    private async void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize Unity Services
            await UnityServices.InitializeAsync();

            AuthenticationService.Instance.SignedIn += () =>
            {
                statusText.text = "Signed in successfully.";
            };

            AuthenticationService.Instance.SignInFailed += (err) =>
            {
                statusText.text = $"Sign-in failed: {err.Message}";
            };

            AuthenticationService.Instance.SignedOut += () =>
            {
                statusText.text = "Signed out successfully.";
            };

            // Attempt automatic login
            await TryAutoLogin();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
    }

    public async void CreateUser()
    {
        string email = emailInput.text;
        string username = usernameInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Email, username, and password cannot be empty.";
            return;
        }

        // Replace with your custom backend logic to create the user
        bool userCreated = await CreateUserOnCustomBackend(email, username, password);

        if (userCreated)
        {
            statusText.text = "User created successfully.";
        }
        else
        {
            statusText.text = "User creation failed.";
        }
    }

    public void Disconnect()
    {
        NetworkManager.Singleton.Shutdown();
        LobbyUI.SetActive(false);
    }

    private async Task<bool> CreateUserOnCustomBackend(string email, string username, string password)
    {
        // Implement your custom backend logic here to create the user
        // This is a placeholder for actual implementation
        await Task.Delay(1000); // Simulate network delay
        return true; // Assume user creation is successful
    }

    public async void SignIn()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Username and password cannot be empty.";
            return;
        }

        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            statusText.text = "Signed in successfully.";

            // Save credentials (or token)
            PlayerPrefs.SetString("username", username);
            PlayerPrefs.SetString("password", password);  // Consider encrypting password before saving
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            statusText.text = $"Sign-in failed: {e.Message}";
        }
    }

    public void SignOut()
    {
        AuthenticationService.Instance.SignOut();
        PlayerPrefs.DeleteKey("username");
        PlayerPrefs.DeleteKey("password");
        PlayerPrefs.Save();
    }

    private async Task TryAutoLogin()
    {
        string savedUsername = PlayerPrefs.GetString("username", null);
        string savedPassword = PlayerPrefs.GetString("password", null);

        if (!string.IsNullOrEmpty(savedUsername) && !string.IsNullOrEmpty(savedPassword))
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                statusText.text = "Auto signed in successfully.";
            }
            catch (Exception e)
            {
                statusText.text = $"Auto sign-in failed: {e.Message}";
            }
        }
    }

    public async void CreateServer()
    {
        string serverName = serverNameInput.text;
        if (string.IsNullOrEmpty(serverName))
        {
            statusText.text = "Server name cannot be empty.";
            return;
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            statusText.text = "You need to sign in first.";
            return;
        }

        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            serverCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();
            statusText.text = $"Server '{serverName}' created with code: {serverCode}";

            lobbyName = serverName;
            lobbyNameText.text = $"{lobbyName} lobby"; // Ustawienie nazwy lobby na elemencie interfejsu
            Debug.Log("Server created with lobby name: " + lobbyName);
        }
        catch (Exception e)
        {
            statusText.text = $"Error creating server: {e.Message}";
        }
    }

    public async void JoinServer()
    {
        string code = serverCodeInput.text;
        if (string.IsNullOrEmpty(code))
        {
            statusText.text = "Server code cannot be empty.";
            return;
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            statusText.text = statusText.text = "You need to sign in first.";
            return;
        }

        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);

            var relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(code);
            NetworkManager.Singleton.StartClient();
        }
        catch (Exception e)
        {
            statusText.text = $"Error joining server: {e.Message}";
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost)
        {
            statusText.text += "\nClient connected: " + clientId;
            SpawnPlayer(clientId);
        }
        else
        {
            statusText.text = "Connected to server!";
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost)
        {
            statusText.text += "\nClient disconnected: " + clientId;
        }
        else
        {
            statusText.text = "Disconnected from server.";
        }
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        if (NetworkManager.Singleton.ConnectedClients.Count >= maxPlayers)
        {
            response.Approved = false;
            response.Reason = "Server is full.";
            statusText.text = "Connection attempt rejected: Server is full.";
        }
        else
        {
            response.Approved = true;
        }
    }

    public void ShowUI()
    {
        LobbyUI.SetActive(true);
    }

    private void SpawnPlayer(ulong clientId)
    {
        GameObject playerObject = Instantiate(playerPrefab);
        playerObject.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

        // Jeśli mamy transform gridu, ustawiamy go jako rodzica dla gracza
        if (gridTransform != null)
        {
            playerObject.transform.SetParent(gridTransform);
        }
    }

    public void OnReadyButtonClicked()
    {
        var localPlayerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (localPlayerObject != null)
        {
            var playerState = localPlayerObject.GetComponent<Player_State>();
            if (playerState != null && playerState.IsOwner)
            {
                playerState.SetReadyServerRpc();
                statusText.text = "You are ready!";
            }
        }
    }
}

