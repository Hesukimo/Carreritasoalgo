using Unity.Netcode;
using UnityEngine;

/// Datos del uso de un objeto, que se pasan al efecto solamente en servidor
public readonly struct PowerUpContext
{
    public readonly ulong UserClientId;
    public readonly NetworkObject UserObject;

    public Vector3 Position => UserObject.transform.position;
    public Vector3 Forward => UserObject.transform.forward;
    public Quaternion Rotation => UserObject.transform.rotation;

    public PowerUpContext(ulong clientId, NetworkObject userObject)
    {
        UserClientId = clientId;
        UserObject = userObject;
    }
}