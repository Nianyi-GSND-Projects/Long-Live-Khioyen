using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Database/Unit Database")]
    public class UnitDefinitionSheet : ScriptableObject
    {
        public List<UnitDefinition> unitDefinitions = new();
    }
}