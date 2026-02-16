using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Definitions/Resource Facility Definition")]
    public class ResourceFacilityDefinition : FacilityDefinition
    {
        [Header("Resource Settings")]
        public ItemDefinition itemToGive;
        public int baseAmount = 10;
        public int durabilityCost = 20;

        public override void OnInteract(Unit user, Facility facility)
        {
            // 1. 计算产出数量 (暂时固定，预留属性挂钩)
            int amount = CalculateOutputAmount(user);

            // 2. 给单位加物品
            if (user is Battalion bat)
            {
                var existingItem = bat.inventory.Find(i => i.definition == itemToGive);
                if (existingItem != null)
                {
                    existingItem.amount += amount;
                }
                else
                {
                    bat.inventory.Add(new inBattleItem { definition = itemToGive, amount = amount });
                }
                
                Debug.Log($"{user.name} 采集了 {amount} 个 {itemToGive.itemName}");
            }

            facility.TakeDamage(durabilityCost);
            
            if (Battle.Instance != null)
            {
                Battle.Instance.CheckDeath(facility);
            }
        }

        private int CalculateOutputAmount(Unit user)
        {
            // TODO: 这里可以读取 user 的属性 (如 Yan 严) 来加成
            return baseAmount;
        }
    }
}