using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPowerUpHolder : NetworkBehaviour
{
    [SerializeField] private Key useKey = Key.E;

    private readonly NetworkVariable<PowerUps> _current = new(
        PowerUps.None,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard[useKey].wasPressedThisFrame)
        {
            Events.RaiseUseInputPressed();
            Debug.Log("presionado E");
        }
            
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            Events.OnPowerUpRolled += HandleRolled;

        if (IsOwner)
        {
            _current.OnValueChanged += OnCurrentChanged;
            Events.OnUseInputPressed += HandleUseInput;
            Events.RaiseLocalPowerUpChanged(_current.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        Events.OnPowerUpRolled -= HandleRolled;
        Events.OnUseInputPressed -= HandleUseInput;
        _current.OnValueChanged -= OnCurrentChanged;
    }

    // ---------- Servidor ----------
    private void HandleRolled(ulong clientId, PowerUps type)
    {
        Debug.Log($"[Holder] Rolled {type} para {clientId}. Yo soy {OwnerClientId}, actual={_current.Value}");
        if (clientId != OwnerClientId) return;
        if (_current.Value != PowerUps.None) return; // ya tiene objeto

        _current.Value = type;
        Events.RaisePowerUpGranted(clientId, type);
    }

    [ServerRpc]
    private void UseServerRpc()
    {
        if (_current.Value == PowerUps.None) return;

        var used = _current.Value;
        _current.Value = PowerUps.None;
        Events.RaisePowerUpUsed(OwnerClientId, used);
    }

    // ---------- Cliente propietario ----------
    private void HandleUseInput() => UseServerRpc();

    private void OnCurrentChanged(PowerUps _, PowerUps current)
        => Events.RaiseLocalPowerUpChanged(current);
}