using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    public enum UnitDatabaseType
    {
        BattalionOnly,
        FacilityOnly,
        All
    }

    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Database/Unit Database")]
    public class UnitDatabase : ScriptableObject
    {
        [Header("Settings")] public UnitDatabaseType databaseType;

        [Header("Data")] public List<UnitDefinition> unitDefinitions = new();

        // 运行时查找字典
        private Dictionary<int, UnitDefinition> _lookup;

        public void Initialize()
        {
            if (_lookup != null) return;
            _lookup = new Dictionary<int, UnitDefinition>();

            foreach (var unit in unitDefinitions)
            {
                if (unit != null && !_lookup.ContainsKey(unit.id))
                {
                    _lookup.Add(unit.id, unit);
                }
            }
        }


        public UnitDefinition GetUnit(int id)
        {
            if (_lookup == null) Initialize();

            if (_lookup.TryGetValue(id, out var unit))
                return unit;

            Debug.LogWarning($"UnitDatabase: ID {id} not found.");
            return null;
        }
    }
}