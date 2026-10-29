using Unity.Netcode;
using UnityEngine;
public class PushReceiver : NetworkBehaviour
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private float drag = 4f; // distancia empujada

    private Vector3 _velocity;

    private void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
    }

    [Rpc(SendTo.Owner)]
    public void ReceivePushRpc(Vector3 impulse)
    {
        impulse.y = 0f;
        _velocity = impulse;
        Debug.Log($"Recibido empuje en direccion {impulse.normalized}");
    }

    private void Update()
    {
        if (!IsOwner || _velocity == Vector3.zero) return;

        controller.Move(_velocity * Time.deltaTime);
        _velocity *= Mathf.Exp(-drag * Time.deltaTime);

        if (_velocity.sqrMagnitude < 0.01f) _velocity = Vector3.zero;
        
    }
}
