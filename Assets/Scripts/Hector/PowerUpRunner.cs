using Unity.Netcode;
using UnityEngine;

public class PowerUpEffectRunner : NetworkBehaviour
{
    // Referencias (prefabs)
    [SerializeField] private NetworkObject soapPrefab;
    [SerializeField] private NetworkObject laxativesPrefab;
    [SerializeField] private float soapMultiplier = 1.5f;
    [SerializeField] private float soapDuration = 3f;
    //...etc

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        Events.OnPowerUpUsed += HandlePowerUpUsed;
    }

    public override void OnNetworkDespawn()
    {
        Events.OnPowerUpUsed -= HandlePowerUpUsed;
    }

    private void HandlePowerUpUsed(ulong clientId, PowerUps type)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)
            || client.PlayerObject == null) return;

        var player = client.PlayerObject;

        switch (type)
        {
            case PowerUps.Laxatives: UseLaxatives(clientId, player); break;
            case PowerUps.Burrito: UseBurrito(clientId, player); break;
            case PowerUps.Soap: UseSoap(clientId, player); break;
            case PowerUps.Weights: UseWeights(clientId, player); break;
        }
    }

    // ---------- Efectos ----------
    private void UseLaxatives(ulong clientId, NetworkObject player)
    {
        Debug.Log("Usado laxantes");
    }

    private void UseBurrito(ulong clientId, NetworkObject player)
    {
        Debug.Log("Usado burrito");
    }

    private void UseSoap(ulong clientId, NetworkObject player)
    {
        Debug.Log("Usado jabón");
    }
    private void UseWeights(ulong clientId, NetworkObject player)
    {
        Debug.Log("Usadas pesas");
    }
}