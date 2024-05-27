using UnityEngine;
using Unity.Netcode;
using System;
using TMPro;

public class Network_UI_Manager : MonoBehaviour
{
    public static Network_UI_Manager instance { get; private set; }

    public TMP_InputField serverNameInput;
    public TMP_InputField serverCodeInput;
    public TextMeshProUGUI statusText;
    private string serverCode;

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public void CreateServer()
    {
        string serverName = serverNameInput.text;
        if (string.IsNullOrEmpty(serverName))
        {
            statusText.text = "Server name cannot be empty.";
            return;
        }

        serverCode = GenerateServerCode();
        NetworkManager.Singleton.StartHost();
        statusText.text = $"Server '{serverName}' created with code: {serverCode}";
    }

    public void JoinServer()
    {
        string code = serverCodeInput.text;
        if (string.IsNullOrEmpty(code))
        {
            statusText.text = "Server code cannot be empty.";
            return;
        }

        NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(code);
        NetworkManager.Singleton.StartClient();
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost)
        {
            statusText.text += "\nClient connected: " + clientId;
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

    private string GenerateServerCode()
    {
        return Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
    }
}
