namespace LongLiveKhioyen
{
    [System.Serializable]
    public class UnitEntryStats
    {
        //状态上限
        public int maxHealth;
        public int maxMorale;
        //随状态变化的属性
        public float repairPower;
        public float attackPower;
        //不随状态变化的属性
        public float defensePower;
        public float flexibility;
        public float discipline;
        public float strategy;
        
        public int cost;
        public int zocPower;
        public int visionRange;
        public int actionChance;
    }
}