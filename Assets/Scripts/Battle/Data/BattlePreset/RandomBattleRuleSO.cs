// Assets/Scripts/Battle/Data/RandomBattleRuleSO.cs (新文件)

using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Level/Random Battle Rule")]
    public class RandomBattleRuleSO : ScriptableObject
    {
        [Header("Available Pools")]
        public List<BattlePoolSO> battlePools = new List<BattlePoolSO>();
        
        [Header("Default Pool")]
        public BattlePoolSO defaultPool;

        public BattlePoolSO GetBattlePool(BattleMetaData metaData)
        {
            // TODO: 在这里实现根据 metaData 选择池子的逻辑
            if (defaultPool != null) return defaultPool;
            
            if (battlePools != null && battlePools.Count > 0)
            {
                return battlePools[0];
            }

            Debug.LogWarning("No BattlePool available in RandomBattleRuleSO!");
            return null;
        }
    }
}