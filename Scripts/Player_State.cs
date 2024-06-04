using UnityEngine;
using Unity.Netcode;
using TMPro;

public class Player_State : NetworkBehaviour
{
    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public TextMeshProUGUI readyText; // Odniesienie do pola tekstowego

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Dodaj logikę do inicjalizacji, jeśli to właściciel obiektu
        }

        // Subskrybuj zmianę wartości isReady
        isReady.OnValueChanged += OnReadyStateChanged;
    }

    [ServerRpc]
    public void SetReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        isReady.Value = true;
        UpdateReadyUIClientRpc();
    }

    private void OnReadyStateChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            UpdateReadyUI();
        }
    }

    [ClientRpc]
    private void UpdateReadyUIClientRpc()
    {
        UpdateReadyUI();
    }

    private void UpdateReadyUI()
    {
        if (readyText != null)
        {
            readyText.text = "READY";
            readyText.color = Color.green;
        }
    }
}
