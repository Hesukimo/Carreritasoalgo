using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Va en el prefab del jugador. Guarda el objeto actual (NetworkVariable escrita
/// solo por el servidor) y gestiona su uso.
/// </summary>
public class PlayerPowerUpHolder : NetworkBehaviour
{
    [SerializeField] private Key useKey = Key.E;

    private readonly NetworkVariable<PowerUps> _current = new(
        PowerUps.None,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        // Todos los clientes: avisar de cambios para el icono sobre la cabeza.
        _current.OnValueChanged += OnCurrentChanged;
        Events.RaisePlayerPowerUpChanged(OwnerClientId, _current.Value); // estado inicial (late joiners)

        if (IsServer)
            Events.OnPowerUpRolled += HandleRolled;

        if (IsOwner)
        {
            Events.OnUseInputPressed += HandleUseInput;
            Events.RaiseLocalPowerUpChanged(_current.Value);
        }
    }
    public void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard[useKey].wasPressedThisFrame)
        {
            Events.RaiseUseInputPressed();
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

    // ---------- Todos ----------
    private void OnCurrentChanged(PowerUps _, PowerUps current)
    {
        Events.RaisePlayerPowerUpChanged(OwnerClientId, current);

        if (IsOwner)
            Events.RaiseLocalPowerUpChanged(current);
    }
}