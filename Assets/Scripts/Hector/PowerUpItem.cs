using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ItemBox : NetworkBehaviour
{
    [Header("Pool")]
    [SerializeField] private PowerUpPool pool;

    [Header("Visual")]
    [SerializeField] private Transform visual;    
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float floatAmplitude = 0.25f;
    [SerializeField] private float floatSpeed = 2f;

    [Header("Respawn")]
    [SerializeField] private float respawnTime = 5f;

    private readonly NetworkVariable<bool> _isAvailable = new(true);
    private Vector3 _visualBasePos;
    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        if (visual != null) _visualBasePos = visual.localPosition;
    }

    public override void OnNetworkSpawn()
    {
        _isAvailable.OnValueChanged += OnAvailabilityChanged;
        ApplyAvailability(_isAvailable.Value);
    }

    public override void OnNetworkDespawn()
    {
        _isAvailable.OnValueChanged -= OnAvailabilityChanged;
    }

    private void OnAvailabilityChanged(bool _, bool current) => ApplyAvailability(current);

    private void ApplyAvailability(bool available)
    {
        if (visual != null) visual.gameObject.SetActive(available);
        if (_collider != null) _collider.enabled = available;
    }

    private void Update()
    {
        if (visual == null || !_isAvailable.Value) return;

        visual.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        visual.localPosition = _visualBasePos +
            Vector3.up * (Mathf.Sin(Time.time * floatSpeed) * floatAmplitude);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[ItemBox] Trigger con {other.name}");
        if (!IsServer || !_isAvailable.Value) return;

        var netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj == null || !netObj.IsPlayerObject) return;
        Debug.Log($"[ItemBox] netObj={(netObj ? netObj.name : "null")} isPlayer={(netObj && netObj.IsPlayerObject)}");
        var data = pool.GetRandomWeighted();
        if (data == null) return;
        Debug.Log($"[ItemBox] Sorteado: {(data ? data.powerUp.ToString() : "NULL")}");
        _isAvailable.Value = false;
        Events.RaisePowerUpRolled(netObj.OwnerClientId, data.powerUp);
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);
        _isAvailable.Value = true;
    }
}