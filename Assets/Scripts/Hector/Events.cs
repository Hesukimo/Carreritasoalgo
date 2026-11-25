using System;

public static class Events
{
    //SERVIDOR
    /// Un jugador ha tocado una caja de objetos.
    public static event Action<ulong> OnItemBoxCollected;

    /// El servidor ha rolleado un objeto para ese cliente.
    public static event Action<ulong, PowerUps> OnPowerUpRolled;

    /// El servidor ha dado efectivamente el objeto al jugador.
    public static event Action<ulong, PowerUps> OnPowerUpGranted;

    /// Un jugador ha usado su objeto (aquí poner efectos reales).
    public static event Action<ulong, PowerUps> OnPowerUpUsed;

    // CLIENTE
    /// El jugador local pulsó el botón de usar objeto.
    public static event Action OnUseInputPressed;

    /// Cambió el objeto que tiene el jugador LOCAL (para la HUD).
    public static event Action<PowerUps> OnLocalPowerUpChanged;

    public static void RaiseItemBoxCollected(ulong clientId) => OnItemBoxCollected?.Invoke(clientId);
    public static void RaisePowerUpRolled(ulong clientId, PowerUps type) => OnPowerUpRolled?.Invoke(clientId, type);
    public static void RaisePowerUpGranted(ulong clientId, PowerUps type) => OnPowerUpGranted?.Invoke(clientId, type);
    public static void RaisePowerUpUsed(ulong clientId, PowerUps type) => OnPowerUpUsed?.Invoke(clientId, type);
    public static void RaiseUseInputPressed() => OnUseInputPressed?.Invoke();
    public static void RaiseLocalPowerUpChanged(PowerUps type) => OnLocalPowerUpChanged?.Invoke(type);
}