using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Level/Random Battle Rule")]
    public class RandomBattleRuleSO : ScriptableObject
    {
        [Header("Battle Pools Matrix [Please keep size to 6 for each]")]
        [Tooltip("无树无水的普通地形地图池 (难度 1~6)")]
        public List<BattlePoolSO> normalPools = new List<BattlePoolSO>(6);
        
        [Tooltip("有树木的地形地图池 (难度 1~6)")]
        public List<BattlePoolSO> treePools = new List<BattlePoolSO>(6);
        
        [Tooltip("有水域的地形地图池 (难度 1~6)")]
        public List<BattlePoolSO> waterPools = new List<BattlePoolSO>(6);
        
        [Tooltip("同时有树木和水域的混合地形地图池 (难度 1~6)")]
        public List<BattlePoolSO> treeAndWaterPools = new List<BattlePoolSO>(6);
        
        [Header("Fallback")]
        [Tooltip("当无法找到对应条件的地图池时使用的默认池")]
        public BattlePoolSO defaultPool;
        
        public BattlePoolSO GetBattlePool(BattleMetaData metaData)
        {
            if (metaData == null)
            {
                Debug.LogWarning("MetaData is null! Returning default pool.");
                return defaultPool;
            }

            bool hasTree = metaData.envParams.tree > 0.5f;
            bool hasWater = metaData.envParams.water > 0.5f;

            List<BattlePoolSO> targetList;
            if (hasTree && hasWater)
            {
                targetList = treeAndWaterPools;
            }
            else if (hasTree)
            {
                targetList = treePools;
            }
            else if (hasWater)
            {
                targetList = waterPools;
            }
            else
            {
                targetList = normalPools;
            }

            int diffIndex = Mathf.Clamp(Mathf.FloorToInt(metaData.envParams.difficulty * 6f), 0, 5);

            if (targetList != null && diffIndex < targetList.Count)
            {
                BattlePoolSO selectedPool = targetList[diffIndex];
                if (selectedPool != null)
                {
                    return selectedPool;
                }
            }

            Debug.LogWarning($"Missing BattlePool for Tree:{hasTree}, Water:{hasWater}, DifficultyIndex:{diffIndex}. Falling back to default.");
            return defaultPool;
        }
    }
}