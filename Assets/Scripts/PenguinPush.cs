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
        [SerializeField] private float pushForce = 12f;   // velocidad inicial
        [SerializeField] private float cooldown = 0.6f;
        [SerializeField] private float maxServerRange = 3f; // validacion

        private float _nextPushTime;
        private readonly Collider[] _hits = new Collider[8];
        private readonly HashSet<ulong> _alreadyPushed = new HashSet<ulong>();

    private void Update()
    {
        if (!IsOwner || Time.time < _nextPushTime) return;

        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (TryPush())
            {
                _nextPushTime = Time.time + cooldown;
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
                if (target == null || target == NetworkObject) continue;          // validacion
            if (!_alreadyPushed.Add(target.NetworkObjectId)) continue;            // validacion

            PushRequestRpc(target, dir);
                pushedAny = true;
            }
            return pushedAny;
        }

        //server
        [Rpc(SendTo.Server)]
        private void PushRequestRpc(NetworkObjectReference targetRef, Vector3 direction)
        {
            if (!targetRef.TryGet(out NetworkObject target)) return;

            float sqr = (target.transform.position - transform.position).sqrMagnitude;
            if (sqr > maxServerRange * maxServerRange) return;

            var receiver = target.GetComponent<PushReceiver>();
            if (receiver != null)
                receiver.ReceivePushRpc(direction.normalized * pushForce);
        }

        private void OnDrawGizmosSelected()
        {
            if (hitbox == null) return;
            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.TRS(hitbox.transform.position, hitbox.transform.rotation, hitbox.transform.lossyScale);
            Gizmos.DrawWireCube(hitbox.center, hitbox.size);
        }
    }
