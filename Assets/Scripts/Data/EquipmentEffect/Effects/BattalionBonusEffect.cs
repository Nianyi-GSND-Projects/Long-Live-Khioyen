using UnityEngine;

namespace LongLiveKhioyen
{
    public class BattalionBonusEffect : EquipmentEffect
    {
        [Header("部队状态值固定加成")]
        public int maxSoldiersBonus;
        public int maxMoraleBonus;

        [Header("部队属性百分比加成")]
        public int attackBonus;
        public int defenceBonus;
        public int flexibilityBonus;
        public int disciplineBonus;
        public int strategyBonus;

        [Header("部队特殊固定加成")] 
        public int movementBonus;
        public int zocPowerBonus;
        public int actionChanceBonus;
        public int visionBonus;
    }
}
