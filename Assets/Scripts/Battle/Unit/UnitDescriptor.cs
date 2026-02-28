namespace LongLiveKhioyen
{
    // 通用描述符基类
    public abstract class UnitDescriptor
    {
        public UnitDefinition Definition;
        public Faction faction;
        public int instanceId = -1;
        
        public bool placed;
        public int maxHealth;
        public int currentHealth;
        // ... 其他通用战前信息
    }

    public class BattalionDescriptor : UnitDescriptor
    {
        public int armyId;
        public GameCommander battalionCommander;
        
        public int maxSolider { get => maxHealth; set => maxHealth = value; }
        public int currentSoliders { get => currentHealth; set => currentHealth = value; }
        
        public int maxMorale;
        public int maxTraining;
        public int currentMurale;
        public int currentTraining;

        public int attackpower;
        public int defencepower;
        public int flexibility;
        public int discipline;
        public int strategy;
    }

    public class FacilityDescriptor : UnitDescriptor
    {
        public bool isConstructed = true;
        public int maxDurability { get => maxHealth; set => maxHealth = value; }
        public int currentDurability { get => currentHealth; set => currentHealth = value; }
        // ... 其他设施特有信息
        
        public int cost; 
    }
}