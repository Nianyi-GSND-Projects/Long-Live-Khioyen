using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public class GrantSkillEffect : EquipmentEffect
    {
        [Header("授予技能")]
        public List<ActionDefinition> skillsToGrant = new List<ActionDefinition>();
        
    }
}