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

    [Header("Jabón")]
    [SerializeField] private LayerMask soapGroundLayers;      // capas de la rampa/suelo
    [SerializeField] private float soapThrowDistance = 6f;    // a cuántos metros hacia atrás cae la mancha
    [SerializeField] private float soapSurfaceOffset = 0.03f; // separación mínima para evitar z-fighting

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

        // Impulso del jugador que utiliza el objeto.
        Events.RaisePlayerKnockback(clientId, -player.transform.forward, 12f, 2f, 0.65f);

        // Buscar la hitbox, incluso si está desactivada.
        BurritoHitbox hitbox = player.GetComponentInChildren<BurritoHitbox>(true);

        if (hitbox != null)
        {
            hitbox.ActivateServer();
        }
        else
        {
            Debug.LogWarning("[Burrito] No se encuentra BurritoHitbox en el Player.");
        }
    }

    private void UseSoap(ulong clientId, NetworkObject player)
    {
        // 1) Superficie bajo el pingüino (para conocer la inclinación de la rampa)
        Vector3 origin = player.transform.position + Vector3.up * 0.5f;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 5f,
                soapGroundLayers, QueryTriggerInteraction.Ignore))
        {
            Debug.LogWarning("[Soap] No hay suelo bajo el jugador.");
            return;
        }

        // 2) "Hacia atrás" pegado a la superficie (cuesta arriba), no en horizontal
        Vector3 back = Vector3.ProjectOnPlane(-player.transform.forward, hit.normal).normalized;
        if (back.sqrMagnitude < 0.0001f) back = Vector3.ProjectOnPlane(Vector3.up, hit.normal).normalized;

        Vector3 point = hit.point + back * soapThrowDistance;
        Vector3 normal = hit.normal;

        // 3) Reproyectar en el punto final por si la rampa cambia de inclinación
        if (Physics.Raycast(point + normal * 2f, -normal, out RaycastHit spot, 6f,
                soapGroundLayers, QueryTriggerInteraction.Ignore))
        {
            point = spot.point;
            normal = spot.normal;
        }

        // 4) Sprite tumbado sobre la superficie: su cara visible (-Z) mira a la normal,
        //    y la parte de arriba del sprite apunta cuesta arriba.
        Vector3 up = Vector3.ProjectOnPlane(back, normal).normalized;
        Quaternion rot = Quaternion.LookRotation(-normal, up);
        Vector3 pos = point + normal * soapSurfaceOffset;

        var stain = Instantiate(soapPrefab, pos, rot);
        stain.GetComponent<SoapStain>().Init(soapMultiplier, soapDuration);
        stain.Spawn();
    }

    private void UseWeights(ulong clientId, NetworkObject player)
    {
        Debug.Log("Usadas pesas");
    }
}