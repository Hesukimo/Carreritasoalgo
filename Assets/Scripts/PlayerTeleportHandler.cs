using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerTeleportHandler : NetworkBehaviour
{
    private CharacterController _characterController;
    private NetworkTransform _networkTransform;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _networkTransform = GetComponent<NetworkTransform>();
    }

    [Rpc(SendTo.Owner)]
    public void TeleportOwnerRpc(Vector3 targetPosition, Quaternion targetRotation)
    {
        // 1. Disable CharacterController so it doesn't fight the new position
        if (_characterController != null)
        {
            _characterController.enabled = false;
        }

        // 2. Move the transform directly
        transform.SetPositionAndRotation(targetPosition, targetRotation);

        // 3. Re-enable the CharacterController
        if (_characterController != null)
        {
            _characterController.enabled = true;
        }

        // 4. Force NetworkTransform to snap without interpolating
        if (_networkTransform != null)
        {
            _networkTransform.Teleport(targetPosition, targetRotation, transform.localScale);
        }
    }
}
