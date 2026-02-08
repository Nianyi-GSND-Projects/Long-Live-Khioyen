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
        [Header("Settings")]
        public UnitDatabaseType databaseType;
        
        [Header("Data")]
        public List<UnitDefinition> unitDefinitions = new();
    }
}