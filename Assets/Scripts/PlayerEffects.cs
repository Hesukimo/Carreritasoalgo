using System;
using StarterAssets;
using Unity.Netcode;
using UnityEngine;

// Un efecto activo sobre un jugador
public struct ActiveEffect : INetworkSerializable, IEquatable<ActiveEffect>
{
    public ushort EffectId;
    public double ExpireTime;      // en tiempo del servidor. Infinito = permanente
    public ulong SourceClientId;   // quien lo aplico (ulong.MaxValue = trampa o entorno)

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref EffectId);
        serializer.SerializeValue(ref ExpireTime);
        serializer.SerializeValue(ref SourceClientId);
    }

    public bool Equals(ActiveEffect o) =>
        EffectId == o.EffectId && ExpireTime == o.ExpireTime && SourceClientId == o.SourceClientId;
}

// Maneja los buffs y debuffs de un jugador.
// - El SERVIDOR agrega y quita efectos.
// - La lista se replica, asi que todos la pueden leer.
// - El DUENO del jugador aplica los stats al ThirdPersonController.
public class PlayerEffects : NetworkBehaviour
{
    [SerializeField] private EffectLibrary library;
    [SerializeField] private ThirdPersonController controller;

    private readonly NetworkList<ActiveEffect> _active = new NetworkList<ActiveEffect>();

    private static readonly int StatCount = Enum.GetValues(typeof(StatType)).Length;
    private readonly float[] _add = new float[StatCount];
    private readonly float[] _mul = new float[StatCount];
    private EffectFlags _flags;
    private bool _localMoveLock; // bloqueo local del movimiento (ej: deslizarse)

    // Valores originales del prefab, para no modificarlos de forma permanente
    private float _baseMove, _baseSprint, _baseJump, _baseGravity;

    // Se lanza en todos los clientes cuando cambia la lista (para UI o VFX)
    public event Action OnEffectsChanged;

    public int Count => _active.Count;
    public ActiveEffect this[int i] => _active[i];
    public bool TryGetDefinition(ushort id, out EffectDefinition def) => library.TryGet(id, out def);

    private void Awake()
    {
        if (controller == null) controller = GetComponent<ThirdPersonController>();
        if (controller != null)
        {
            _baseMove = controller.MoveSpeed;
            _baseSprint = controller.SprintSpeed;
            _baseJump = controller.JumpHeight;
            _baseGravity = controller.Gravity;
        }
        ResetCache();
    }

    public override void OnNetworkSpawn()
    {
        _active.OnListChanged += OnListChanged;
        Recalculate();
    }

    public override void OnNetworkDespawn()
    {
        _active.OnListChanged -= OnListChanged;
    }

    private void OnListChanged(NetworkListEvent<ActiveEffect> _) => Recalculate();

    private void Update()
    {
        if (!IsServer || !IsSpawned) return;

        // El servidor quita los efectos que ya expiraron
        double now = NetworkManager.ServerTime.Time;
        for (int i = _active.Count - 1; i >= 0; i--)
            if (_active[i].ExpireTime <= now) _active.RemoveAt(i);
    }

    // ---------- Consultas (cualquier cliente) ----------

    // (valor base + sumas) * multiplicadores
    public float Get(StatType stat, float baseValue) => (baseValue + _add[(int)stat]) * _mul[(int)stat];

    // Solo el multiplicador total
    public float Multiplier(StatType stat) => _mul[(int)stat];

    public bool HasAnyFlag(EffectFlags mask) => (_flags & mask) != 0;

    public bool Has(ushort effectId)
    {
        for (int i = 0; i < _active.Count; i++)
            if (_active[i].EffectId == effectId) return true;
        return false;
    }

    // Segundos que le quedan al efecto en esa posicion de la lista
    public float RemainingTime(int index)
    {
        double t = _active[index].ExpireTime;
        return double.IsInfinity(t) ? float.PositiveInfinity : Mathf.Max(0f, (float)(t - NetworkManager.ServerTime.Time));
    }

    // ---------- API del servidor ----------

    // Aplica un efecto. Solo llamar desde el servidor (trampas, pickups, poderes).
    public bool ServerApply(ushort effectId, ulong sourceClientId = ulong.MaxValue, float durationOverride = -1f)
    {
        if (!IsServer) { Debug.LogWarning("ServerApply se llamo fuera del servidor."); return false; }
        if (!library.TryGet(effectId, out var def)) { Debug.LogWarning($"Efecto desconocido: {effectId}"); return false; }
        if (def.isDebuff && HasAnyFlag(EffectFlags.DebuffImmune)) return false;

        float duration = durationOverride > 0f ? durationOverride : def.defaultDuration;
        double expire = duration > 0f ? NetworkManager.ServerTime.Time + duration : double.PositiveInfinity;

        // Contar copias activas y buscar la mas antigua
        int count = 0, oldest = -1;
        double oldestExpire = double.MaxValue;
        for (int i = 0; i < _active.Count; i++)
        {
            if (_active[i].EffectId != effectId) continue;
            count++;
            if (_active[i].ExpireTime < oldestExpire) { oldestExpire = _active[i].ExpireTime; oldest = i; }
        }

        if (count >= Mathf.Max(1, def.maxStacks))
        {
            // Limite alcanzado: refrescar la mas antigua
            var e = _active[oldest];
            e.ExpireTime = expire;
            e.SourceClientId = sourceClientId;
            _active[oldest] = e;
        }
        else
        {
            _active.Add(new ActiveEffect { EffectId = effectId, ExpireTime = expire, SourceClientId = sourceClientId });
        }
        return true;
    }

    public void ServerRemove(ushort effectId)
    {
        if (!IsServer) return;
        for (int i = _active.Count - 1; i >= 0; i--)
            if (_active[i].EffectId == effectId) _active.RemoveAt(i);
    }

    // Quita todos los debuffs (limpiar)
    public void ServerClearDebuffs()
    {
        if (!IsServer) return;
        for (int i = _active.Count - 1; i >= 0; i--)
            if (library.TryGet(_active[i].EffectId, out var d) && d.isDebuff) _active.RemoveAt(i);
    }

    public void ServerClearAll()
    {
        if (IsServer) _active.Clear();
    }

    // Aplica un efecto a todos los jugadores menos a uno (ej: ralentizar a los rivales)
    public static void ServerApplyToOthers(ulong exceptClientId, ushort effectId, float durationOverride = -1f)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;

        foreach (var client in nm.ConnectedClientsList)
        {
            if (client.ClientId == exceptClientId || client.PlayerObject == null) continue;
            if (client.PlayerObject.TryGetComponent(out PlayerEffects fx))
                fx.ServerApply(effectId, exceptClientId, durationOverride);
        }
    }

    // Bloquea o desbloquea caminar y saltar solo en el dueno (lo usa PenguinSlide)
    public void SetLocalMoveLock(bool locked)
    {
        if (_localMoveLock == locked) return;
        _localMoveLock = locked;
        if (IsOwner) ApplyToController();
    }

    // ---------- Interno ----------

    private void ResetCache()
    {
        Array.Clear(_add, 0, _add.Length);
        for (int i = 0; i < _mul.Length; i++) _mul[i] = 1f;
        _flags = EffectFlags.None;
    }

    // Recalcula los stats sumando todos los efectos activos
    private void Recalculate()
    {
        ResetCache();

        for (int i = 0; i < _active.Count; i++)
        {
            if (!library.TryGet(_active[i].EffectId, out var def)) continue;
            _flags |= def.flags;
            foreach (var m in def.modifiers)
            {
                if (m.op == ModOp.Add) _add[(int)m.stat] += m.value;
                else _mul[(int)m.stat] *= m.value;
            }
        }

        if (IsOwner) ApplyToController();
        OnEffectsChanged?.Invoke();
    }

    // Pasa los stats calculados al ThirdPersonController (solo el dueno)
    private void ApplyToController()
    {
        if (controller == null) return;

        if (HasAnyFlag(EffectFlags.Stunned) || _localMoveLock)
        {
            controller.MoveSpeed = 0f;
            controller.SprintSpeed = 0f;
            controller.JumpHeight = 0f;
        }
        else
        {
            controller.MoveSpeed = Mathf.Max(0f, Get(StatType.MoveSpeed, _baseMove));
            controller.SprintSpeed = Mathf.Max(0f, Get(StatType.SprintSpeed, _baseSprint));
            controller.JumpHeight = Mathf.Max(0f, Get(StatType.JumpHeight, _baseJump));
        }
        controller.Gravity = Get(StatType.Gravity, _baseGravity);
    }
}
