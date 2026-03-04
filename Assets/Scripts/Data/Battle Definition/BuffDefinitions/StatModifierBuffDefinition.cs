using System.Collections.Generic;
using UnityEngine;
using System;
namespace LongLiveKhioyen
{
    public enum ModifierType
    {
        Additive,      // 加法 (例如, +10 攻击力)
        Multiplicative // 乘法 (例如, *1.2 攻击力, 此处 Value 应为 1.2)
    }

    [Serializable]
    public class StatModifier
    {
        public StatType StatToModify;
        public ModifierType Type;
        public float Value;
    }
    [Serializable]
    public class StatModifierBuffDefinition : BuffDefinition
    {
        public List<StatModifier> Modifiers = new List<StatModifier>();
    }
}