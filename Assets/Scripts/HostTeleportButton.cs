using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class HostTeleportButton : NetworkBehaviour
{
    public Transform targetPosition;

    void Update()
    {
        if(Keyboard.current.tKey.wasPressedThisFrame)
        {
            TeleportAllPlayers();
        }
    }


    public void TeleportAllPlayers()
    {
        // Only the Host/Server can initiate this
        if (!IsServer) return;

        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null && 
                client.PlayerObject.TryGetComponent(out PlayerTeleportHandler teleportHandler))
            {
                teleportHandler.TeleportOwnerRpc(
                    targetPosition.position, 
                    targetPosition.rotation
                );
            }
        }
    }
}
