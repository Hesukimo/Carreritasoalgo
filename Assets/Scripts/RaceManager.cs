using Unity.Netcode;
using UnityEngine;

public class RaceManager : NetworkBehaviour
{
    public static RaceManager Instance { get; private set; }

    // Variable que determina si la carrera ha terminado
    public static bool GameOver => Instance != null && Instance.GameOverState.Value;

    // Clientes que ya han llegado a la meta.
    public NetworkList<ulong> ArrivedPenguins = new NetworkList<ulong>();

    // Estado sincronizado de la carrera.
    public NetworkVariable<bool> GameOverState = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ID del último pingüino
    public NetworkVariable<ulong> WinnerID = new NetworkVariable<ulong>(ulong.MaxValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        Instance = this;
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this) { Instance = null; }
    }

    public void RegistrarLlegada(NetworkObject player)
    {
        // Calcular llegadas en el servidor solo!
        if (!IsServer || GameOver || player == null) { return; }

        ulong clientId = player.OwnerClientId;

        // No contar al mismo pingüino
        if (ArrivedPenguins.Contains(clientId)) { return; }
        ArrivedPenguins.Add(clientId);
        int jugadoresTotales = NetworkManager.Singleton.ConnectedClientsIds.Count;

        Debug.Log($"Penguin {clientId} got to the goal " + $"{ArrivedPenguins.Count} out of {jugadoresTotales}");

        // Establecer ID del ganador en caso de que los demás hayan llegado
        if (ArrivedPenguins.Count >= jugadoresTotales)
        {
            WinnerID.Value = clientId;
            GameOverState.Value = true;

            Debug.Log($"Congrats {clientId} on not dying!");
        }
    }

    public bool HasArrived(ulong clientId)
    {
        return ArrivedPenguins.Contains(clientId);
    }
}