using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Definitions/Trap Facility")]
    public class TrapFacilityDefinition : FacilityDefinition
    {
        [Header("Trap Settings")]
        [Tooltip("对进入的单位造成的伤害")]
        public int damage = 100;

        [Tooltip("触发后是否销毁自身")]
        public bool consumeOnTrigger = true;

        [Tooltip("是否对敌方单位触发")]
        public bool triggerOnEnemies = true;

        [Tooltip("是否对友方单位触发")]
        public bool triggerOnAllies = false;
        
        [Tooltip("是否阻止移动")]
        public bool PreventMovement = true;
        
        public void Trigger(Unit target, Facility trapInstance)
        {
            if (target == null || trapInstance == null) return;

            bool shouldTrigger = (triggerOnEnemies && target.faction != trapInstance.faction) ||
                                 (triggerOnAllies && target.faction == trapInstance.faction);

            if (shouldTrigger)
            {
                Debug.Log($"{target.name} 踩中了陷阱 {trapInstance.name}!");
                
                target.TakeDamage(damage, trapInstance);
                
                if (consumeOnTrigger)
                {
                    trapInstance.currentDurability = -1;
                    Battle.Instance.MarkUnitDirty(trapInstance);
                    Battle.Instance.CheckDeath(trapInstance);
                }
            }
        }
    }
}