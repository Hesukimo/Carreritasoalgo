using System;
using UnityEngine;

//ESTE CODE ES PARA EL FEEDBACK VISUAL TODAVIA TENGO QUE IMPLEMENTARLO Y ENTENDERLO PORQUE ES PURO CHATI, IGNORARLO
public enum StatType
{
    MoveSpeed,
    SprintSpeed,
    JumpHeight,
    Gravity,
    PushForce,      // fuerza de empujones
    PushCooldown,   // tiempo entre empujones
    PushTaken       // multiplicador de los empujones que recibe este jugador
}

public enum ModOp { Add, Multiply }

[Flags]
public enum EffectFlags
{
    None         = 0,
    Stunned      = 1 << 0, // no puede moverse, saltar ni empujar
    CannotPush   = 1 << 1, // no puede empujar
    PushImmune   = 1 << 2, // inmune a empujones
    DebuffImmune = 1 << 3  // escudo: bloquea debuffs nuevos
}

[Serializable]
public struct StatModifier
{
    public StatType stat;
    public ModOp op;
    public float value; // Multiply: 0.5 = mitad, 2 = doble. Add: cantidad fija.
}

// Describe un buff o debuff. Crear con: Assets > Create > Penguin > Effect Definition
[CreateAssetMenu(menuName = "Penguin/Effect Definition")]
public class EffectDefinition : ScriptableObject
{
    [Tooltip("Unico y distinto de 0. Es lo que se envia por red.")]
    public ushort id = 1;
    public string displayName = "Nuevo Efecto";
    public bool isDebuff;
    [Tooltip("Segundos. 0 o menos = dura hasta que se quite.")]
    public float defaultDuration = 3f;
    [Tooltip("Cuantas copias a la vez. Al llegar al limite, se refresca la mas antigua.")]
    [Min(1)] public int maxStacks = 1;
    public EffectFlags flags;
    public StatModifier[] modifiers;

    [Header("Visual (opcional)")]
    public Sprite icon;
    public GameObject vfxPrefab;
}
