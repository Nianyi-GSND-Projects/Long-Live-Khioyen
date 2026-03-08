// Assets/Scripts/Data/Battle Definition/FacilityDefinitions/TreasureChestDefinition.cs

using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Definitions/Treasure Chest Definition")]
    public class TreasureChestDefinition : FacilityDefinition
    {
        [Header("Treasure Settings")]
        // [移除] public LootTableSO lootTable; // 不再需要，使用基类的 lootRules
        public GameObject openVfxPrefab;
        
        [Tooltip("开启后是否直接销毁宝箱")]
        public bool destroyOnOpen = true;

        public override void OnInteract(Unit user, Facility facility)
        {
            if (user == null || facility == null) return;

            Debug.Log($"{user.name} 打开了宝箱 {facility.name}");

            // 1. 遍历所有掉落规则
            if (lootRules != null)
            {
                foreach (var rule in lootRules)
                {
                    if (rule.lootTable == null) continue;

                    // 检查概率
                    if (Random.Range(0, 100) < rule.dropChance)
                    {
                        // 从表中 Roll 出物品
                        var loot = rule.lootTable.Roll();
                        
                        if (loot != null && loot.definition != null && user is Battalion bat)
                        {
                            bat.AddItem(loot.definition, loot.amount);
                        }
                    }
                }
            }

            // 2. 播放特效
            if (openVfxPrefab != null)
            {
                if (Battle.Instance != null)
                {
                    Vector3 worldPos = Battle.Instance.MapToWorld(facility.position);
                    Instantiate(openVfxPrefab, worldPos, Quaternion.identity);
                }
            }

            // 3. 销毁宝箱
            if (destroyOnOpen)
            {
                if (Battle.Instance != null)
                {
                    Battle.Instance.RemoveUnitFromBattle(facility);
                }
            }
        }
    }
}