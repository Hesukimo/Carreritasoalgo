using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class HostTeleportButton : NetworkBehaviour
{
    public Transform[] playerPositions;

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
                // Get the player's OwnerClientId
                ulong ownerId = client.ClientId;

                // Check that the ID is within the array bounds
                if ((int)ownerId >= playerPositions.Length) continue;

                // Get the position corresponding to the OwnerID
                Transform target = playerPositions[ownerId];

                teleportHandler.TeleportOwnerRpc(
                    target.position,
                    target.rotation
                );
            }
        }
    }
}
