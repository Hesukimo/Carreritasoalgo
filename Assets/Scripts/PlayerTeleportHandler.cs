using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerTeleportHandler : NetworkBehaviour
{
    private CharacterController _characterController;
    private NetworkTransform _networkTransform;
    private PenguinSlide _slide;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _networkTransform = GetComponent<NetworkTransform>();
        _slide = GetComponent<PenguinSlide>();
    }

    [Rpc(SendTo.Owner)]
    public void TeleportOwnerRpc(Vector3 targetPosition, Quaternion targetRotation)
    {
        // 0. Cancelar el deslizamiento para no llevar velocidad al nuevo sitio
        if (_slide != null) _slide.ResetSlide();

        // 1. Desactivar el CharacterController para que no pelee con la nueva posicion
        if (_characterController != null)
        {
            _characterController.enabled = false;
        }

        // 2. Mover el transform directamente
        transform.SetPositionAndRotation(targetPosition, targetRotation);

        // 3. Volver a activar el CharacterController
        if (_characterController != null)
        {
            _characterController.enabled = true;
        }

        // 4. Forzar al NetworkTransform a saltar sin interpolar
        if (_networkTransform != null)
        {
            _networkTransform.Teleport(targetPosition, targetRotation, transform.localScale);
        }
    }
}
