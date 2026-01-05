using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Commander/Preset Template")]
    public class CommanderTemplateSO : ScriptableObject
    {
        [Header("Basic Info")]
        public string commanderName;
        public Sprite portrait;
        
        [Header("Stats")]
        [Range(1, 100)] public int Zhi = 50;
        [Range(1, 100)] public int Xin = 50;
        [Range(1, 100)] public int Ren = 50;
        [Range(1, 100)] public int Yong = 50;
        [Range(1, 100)] public int Yan = 50;
        
        public GameCommander CreateInstance(int newId)
        {
            return new GameCommander()
            {
                commanderId = newId,
                commanderName = commanderName,
                portrait = portrait,
                Zhi = Zhi,
                Xin = Xin,
                Ren = Ren,
                Yong = Yong,
                Yan = Yan,
                isAssigned = false
            };
        }
    }
}