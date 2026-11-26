using Unity.Netcode;
using UnityEngine;

public class PlayerPowerUpIcon : MonoBehaviour
{
    [SerializeField] private PowerUpPool database;
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private bool hideForLocalPlayer = false;

    private NetworkObject _networkObject;
    private Camera _cam;

    private void Awake()
    {
        _networkObject = GetComponentInParent<NetworkObject>();
        SetIcon(null);
    }

    // Nos suscribimos en OnEnable (antes del spawn de red) para no perder el evento inicial en OnNetworkSpawn
    private void OnEnable() => Events.OnPlayerPowerUpChanged += HandleChanged;
    private void OnDisable() => Events.OnPlayerPowerUpChanged -= HandleChanged;

    private void HandleChanged(ulong clientId, PowerUps type)
    {
        if (_networkObject == null || !_networkObject.IsSpawned) return;
        if (clientId != _networkObject.OwnerClientId) return;

        if (hideForLocalPlayer && _networkObject.IsOwner)
        {
            SetIcon(null);
            return;
        }

        var data = type == PowerUps.None ? null : database.Get(type);
        SetIcon(data != null ? data.sprite : null);
    }

    private void SetIcon(Sprite sprite)
    {
        iconRenderer.sprite = sprite;
        iconRenderer.enabled = sprite != null;
    }

    // Apuntar hacia la cámara
    private void LateUpdate()
    {
        if (!iconRenderer.enabled) return;

        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        iconRenderer.transform.rotation = _cam.transform.rotation;
    }
}