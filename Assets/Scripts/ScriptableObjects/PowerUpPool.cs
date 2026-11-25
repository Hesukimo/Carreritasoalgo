using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PowerUpDatabase", menuName = "Scriptable Objects/Power Up Pool")]
public class PowerUpPool : ScriptableObject
{
    [SerializeField] private List<PowerUp> powerUps = new();

    private Dictionary<PowerUps, PowerUp> _lookup;

    public PowerUp Get(PowerUps type)
    {
        if (_lookup == null)
        {
            _lookup = new Dictionary<PowerUps, PowerUp>();
            foreach (var p in powerUps)
                if (p != null) _lookup[p.powerUp] = p;
        }
        return _lookup.TryGetValue(type, out var data) ? data : null;
    }

    /// Selección aleatoria ponderada por peso
    public PowerUp GetRandomWeighted()
    {
        int total = 0;
        foreach (var p in powerUps)
        {
            if (p == null) continue;
            Debug.Log($"{p.name} id={p.powerUp} weight={p.peso}");
            total += p.peso;
        }

        if (total <= 0) return null;

        int roll = Random.Range(0, total);
        Debug.Log($"total={total} roll={roll}");

        foreach (var p in powerUps)
        {
            if (p == null) continue;
            roll -= p.peso;
            if (roll < 0) return p;
        }
        return null;
    }

#if UNITY_EDITOR
    private void OnValidate() => _lookup = null;
#endif
}