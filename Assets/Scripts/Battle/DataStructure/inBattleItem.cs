using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public class inBattleItem
    {
        public ItemDefinition definition;
        public int amount = 1;
        
        public float GetTotalWeight()
        {
            if (definition == null)
            {
                Debug.LogWarning($"缺少 ItemDefinition，无法计算重量！");
                return 0f;
            }

            return definition.itemWeightFactor * amount;
        }
        
    }
}
