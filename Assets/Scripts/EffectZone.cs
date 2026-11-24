using Unity.Netcode;
using UnityEngine;

// Trampa o pickup
//SERVER SIDE
public class EffectZone : MonoBehaviour
{
    [SerializeField] private EffectDefinition effect;
    [Tooltip("-1 = usar la duracion por defecto del efecto")]
    [SerializeField] private float durationOverride = -1f;
    [SerializeField] private bool consumeOnUse;        // pickup desaparece al usarse
    [SerializeField] private float reuseDelay = 0.5f;  // validacion para que no se active el mismo coso varias veces

    private float _nextUse;

    private void OnTriggerEnter(Collider other)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer || effect == null) return;
        if (Time.time < _nextUse) return;

        var fx = other.GetComponentInParent<PlayerEffects>();
        if (fx == null) return;

        if (fx.ServerApply(effect.id, ulong.MaxValue, durationOverride))
        {
            _nextUse = Time.time + reuseDelay;
            if (consumeOnUse)
            {
                NetworkObject networkObject = GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned)
                {
                    networkObject.Despawn();
                }
            }
        }
    }
}
