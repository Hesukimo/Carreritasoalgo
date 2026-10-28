using Unity.Netcode;
using UnityEngine;

public class Goal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (NetworkManager.Singleton == null) return;

        // El servidor registra llegadas
        if (!NetworkManager.Singleton.IsServer) return;
        if (RaceManager.Instance == null) return;
        if (RaceManager.GameOver) return;

        NetworkObject player = other.GetComponentInParent<NetworkObject>();

        if (player == null) { return; }
        RaceManager.Instance.RegistrarLlegada(player);
    }
}