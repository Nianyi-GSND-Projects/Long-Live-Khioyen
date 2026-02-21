using UnityEngine;
using NaughtyAttributes;

namespace LongLiveKhioyen
{
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum ItemType
    {
        Iron, //金 金属、贵金属
        Wood, //木 木材、粮草
        People,//水 人、液体
        Stone,//土 土石
        Weapon //火 兵戈、燃料
    }
    [CreateAssetMenu(menuName = "Long Live Khioyen/Item/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Database Info")]
        public int id;
        
        [Header("Identity")]
        public string itemId;
        public string itemName;
        
        public string[] tags;
        public string description;
        
        [Header("Visual")]
        public Sprite icon;
        
        [Header("Value")]
        public Rarity rarity;
        
        public float value;
        //每一个该物品在贸易中的参考价值
        
        [Header("Stats")]
        public float itemWeightFactor = 1;
        //同一个堆叠中，每一个该物品提供的重量数值
        
        public int maxStackNumber = 100;
        //该物品最大可以在同一格内堆叠多少个

        [Header("Production")]
        public bool productable = false;
        public ResourceDescriptor[] costs;

        [Header("Trading")]
        public bool canSell = true;
        [ShowIf("canSell"), Min(0)] public float sellPrice;
        public bool canBuy = true;
        [ShowIf("canBuy"), Min(0)] public float buyPrice;
    }
}
