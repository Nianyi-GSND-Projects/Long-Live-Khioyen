using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Commander/Data/Personality")]
    public class CommanderPersonalitySO : ScriptableObject
    {
        [TextArea] public string description;

        [Header("Fixed Growth Weights (Total 5 points distributed by weight)")]
        [Range(0, 100)] public int zhiWeight = 20;
        [Range(0, 100)] public int xinWeight = 20;
        [Range(0, 100)] public int renWeight = 20;
        [Range(0, 100)] public int yongWeight = 20;
        [Range(0, 100)] public int yanWeight = 20;

        [Header("Extra Growth Chance (0-100%)")]
        [Range(0, 100)] public int zhiChance = 10;
        [Range(0, 100)] public int xinChance = 10;
        [Range(0, 100)] public int renChance = 10;
        [Range(0, 100)] public int yongChance = 10;
        [Range(0, 100)] public int yanChance = 10;
        
        public int TotalWeight => zhiWeight + xinWeight + renWeight + yongWeight + yanWeight;
    }
}