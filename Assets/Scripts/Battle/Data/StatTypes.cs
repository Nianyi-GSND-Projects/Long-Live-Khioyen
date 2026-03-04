namespace LongLiveKhioyen
{
    public enum StatType
    {
        // 主要战斗属性
        AttackPower,    // 攻击力
        DefensePower,   // 防御力
        RepairPower,    // 修补能力

        // 次要单位属性
        Flexibility,    // 机动力
        Discipline,     // 纪律
        Strategy,       // 策略

        // 最大值属性
        MaxHealth,      // 最大生命/兵力
        MaxMorale,      // 最大士气

        // 衍生属性
        Movement,       // 移动力
        ZocPower,       // 控制力
        DamageResistance, // 伤害抵抗
        ActionChance
    }
}