using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/TileEffects/Fire")]
    public class DamageTileEffect : TileEffectDefinition
    {
        public int damagePerTurn = 100;
        public bool ignoreDefense = false;

        protected override void ApplyEffectToUnit(Unit unit)
        {
            Debug.Log($"{unit.name} 在火中受到灼烧！");
            unit.TakeDamage(damagePerTurn);
        }
    }
}
