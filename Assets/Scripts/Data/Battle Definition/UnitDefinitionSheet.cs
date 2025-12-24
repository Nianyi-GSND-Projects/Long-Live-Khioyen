using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Unit Definition Sheet")]
    public class UnitDefinitionSheet : ScriptableObject
    {
        public List<UnitDefinition> unitDefinitions = new();
    }
}