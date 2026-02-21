using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Commander/Preset Template")]
    public class CommanderTemplateSO : ScriptableObject
    {
        [Header("Basic Info")]
        public string commanderName;
        public Sprite portrait;
        public Race race;
        [Min(1)] public int level = 1;
        
        [Header("Stats")]
        [Range(1, 100)] public int Zhi = 50;
        [Range(1, 100)] public int Xin = 50;
        [Range(1, 100)] public int Ren = 50;
        [Range(1, 100)] public int Yong = 50;
        [Range(1, 100)] public int Yan = 50;
        
        [Header("Traits & Skills")]
        public CommanderPersonalitySO personality;
        public List<CommanderTraitSO> traits;
        public List<ActionDefinition> commanderActions;
        
        public GameCommander CreateInstance(int newId)
        {
            return new GameCommander()
            {
                commanderId = newId,
                commanderName = commanderName,
                portrait = portrait,
                race = race,
                level = level,
            
                Zhi = Zhi,
                Xin = Xin,
                Ren = Ren,
                Yong = Yong,
                Yan = Yan,
            
                personality = personality,
                traits = new List<CommanderTraitSO>(traits),
                commanderActions = new List<ActionDefinition>(commanderActions),
            
                isAssigned = false
            };
        }
    }
}