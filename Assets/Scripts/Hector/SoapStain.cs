using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Mancha de jabón. Prefab con: NetworkObject, SoapStain, un Collider (isTrigger)
/// y un Rigidbody kinematic. El sprite va en un hijo.
/// Solo el servidor detecta a los pingüinos y despawnea la mancha.
/// </summary>
public class SoapStain : NetworkBehaviour
{
    [SerializeField] private float lifetime = 10f;

    private float _multiplier = 1.5f;
    private float _boostDuration = 3f;

    /// Llamar en el servidor ANTES de Spawn()
    public void Init(float multiplier, float boostDuration)
    {
        _multiplier = multiplier;
        _boostDuration = boostDuration;
    }

    public override void OnNetworkSpawn()
    {
        // Nada más spawnear en red iniciamos una cuenta atrás en el servidor para despawnearnos a nosotros mismos
        if (IsServer) Invoke(nameof(DespawnSelf), lifetime);
    }

    public override void OnNetworkDespawn()
    {
        CancelInvoke();
    }

    private void DespawnSelf()
    {
        if (IsSpawned) NetworkObject.Despawn(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        var netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj == null || !netObj.IsPlayerObject) return;

        Events.RaiseSpeedBoost(netObj.OwnerClientId, _multiplier, _boostDuration);
    }
}