using System.Collections.Generic;
using UnityEngine;
//TESTING TAMBIEN DEL CHATI

// Lista de todos los efectos. Sirve para buscar un efecto por su id.
[CreateAssetMenu(menuName = "Penguin/Effect Library")]
public class EffectLibrary : ScriptableObject
{
    [SerializeField] private EffectDefinition[] effects;
    private Dictionary<ushort, EffectDefinition> _map;

    private void OnEnable() => _map = null;

    public bool TryGet(ushort id, out EffectDefinition def)
    {
        if (_map == null)
        {
            _map = new Dictionary<ushort, EffectDefinition>();
            foreach (var e in effects)
            {
                if (e == null) continue;
                if (_map.ContainsKey(e.id))
                    Debug.LogError($"Id de efecto repetido: {e.id} en '{e.name}'", e);
                else
                    _map.Add(e.id, e);
            }
        }
        return _map.TryGetValue(id, out def);
    }
}
