namespace LongLiveKhioyen
{
    // 通用描述符基类
    public class UnitDescriptor
    {
        public UnitDefinition Definition;
        public Faction faction;
        public int instanceId = -1;
        
        // ... 其他通用战前信息
    }

    public class BattalionDescriptor : UnitDescriptor
    {
        public int armyId;
        public GameCommander battalionCommander;
        public int maxSolider;
        public int maxMorale;
        public int maxTraining;
        public int currentSoliders;
        public int currentMurale;
        public int currentTraining;

        public int attackpower;
        public int defencepower;
        public int flexibility;
        public int discipline;
        public int strategy;
        
        public bool placed;
    }

    public class FacilityDescriptor : UnitDescriptor
    {
        public int currentDurability;
        public int maxDurability;
        // ... 其他设施特有信息
        
        public int cost; 
    }
}