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
            int amount = CalculateOutputAmount(user);

            if (user is Battalion bat)
            {
                bat.AddItem(itemToGive, amount);
            }
            else return;

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