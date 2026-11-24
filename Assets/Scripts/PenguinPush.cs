using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PenguinPush : NetworkBehaviour
{
    [Header("Hitbox")]
    [SerializeField] private BoxCollider hitbox;
    [SerializeField] private LayerMask playerLayers;

    [Header("Empujon")]
    [SerializeField] private float pushForce = 12f;     // velocidad inicial
    [SerializeField] private float cooldown = 0.6f;
    [SerializeField] private float maxServerRange = 3f; // validacion

    [Header("Efectos")]
    [SerializeField] private PlayerEffects effects;

    private float _nextPushTime;
    private readonly Collider[] _hits = new Collider[8];
    private readonly HashSet<ulong> _alreadyPushed = new HashSet<ulong>();

    private void Awake()
    {
        if (effects == null) effects = GetComponent<PlayerEffects>();
    }

    //Update
    private void Update()
    {
        if (!IsOwner || Time.time < _nextPushTime) return;

        // Aturdido o sin poder empujar
        if (effects != null && effects.HasAnyFlag(EffectFlags.Stunned | EffectFlags.CannotPush)) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (TryPush())
            {
                float cd = effects != null ? effects.Get(StatType.PushCooldown, cooldown) : cooldown;
                _nextPushTime = Time.time + Mathf.Max(0.05f, cd);
            }
        }
    }

    private bool TryPush()
    {
        Transform t = hitbox.transform;
        Vector3 center = t.TransformPoint(hitbox.center);
        Vector3 half = Vector3.Scale(hitbox.size, t.lossyScale) * 0.5f;
        half = new Vector3(Mathf.Abs(half.x), Mathf.Abs(half.y), Mathf.Abs(half.z));

        int count = Physics.OverlapBoxNonAlloc(center, half, _hits, t.rotation,
            playerLayers, QueryTriggerInteraction.Ignore);

        Vector3 dir = transform.forward;
        dir.y = 0f;
        dir.Normalize();

        _alreadyPushed.Clear();
        bool pushedAny = false;
        for (int i = 0; i < count; i++)
        {
            var target = _hits[i].GetComponentInParent<NetworkObject>();
            if (target == null || target == NetworkObject) continue; // ignorar a uno mismo
            if (!_alreadyPushed.Add(target.NetworkObjectId)) continue; // no repetir objetivo

            PushRequestRpc(target, dir);
            pushedAny = true;
        }
        return pushedAny;
    }

    // Servidor
    [Rpc(SendTo.Server)]
    private void PushRequestRpc(NetworkObjectReference targetRef, Vector3 direction)
    {
        if (!targetRef.TryGet(out NetworkObject target)) return;

        // Validar al atacante
        if (effects != null && effects.HasAnyFlag(EffectFlags.Stunned | EffectFlags.CannotPush)) return;

        float sqr = (target.transform.position - transform.position).sqrMagnitude;
        if (sqr > maxServerRange * maxServerRange) return;

        float force = effects != null ? effects.Get(StatType.PushForce, pushForce) : pushForce;

        // Efectos del objetivo: inmunidad y multiplicador de empuje recibido
        if (target.TryGetComponent(out PlayerEffects targetFx))
        {
            if (targetFx.HasAnyFlag(EffectFlags.PushImmune)) return;
            force *= targetFx.Multiplier(StatType.PushTaken);
        }

        var receiver = target.GetComponent<PushReceiver>();
        if (receiver != null)
            receiver.ReceivePushRpc(direction.normalized * force);
    }

    private void OnDrawGizmosSelected()
    {
        if (hitbox == null) return;
        Gizmos.color = Color.red;
        Gizmos.matrix = Matrix4x4.TRS(hitbox.transform.position, hitbox.transform.rotation, hitbox.transform.lossyScale);
        Gizmos.DrawWireCube(hitbox.center, hitbox.size);
    }
}
