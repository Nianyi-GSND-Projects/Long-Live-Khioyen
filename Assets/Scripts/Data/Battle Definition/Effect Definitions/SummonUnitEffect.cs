// Assets/Scripts/Data/Battle Definition/Effect Definitions/SummonUnitEffect.cs

using UnityEngine;
using System;

namespace LongLiveKhioyen
{
    [Serializable]
    public class SummonUnitEffect : EffectDefinition
    {
        [Header("Unit To Summon")]
        public BattalionDefinition battalionDef;
        
        [Header("Commander Settings")]
        [Tooltip("如果指定了模板，将使用此模板创建指挥官")]
        public CommanderTemplateSO commanderTemplate;
        
        [Tooltip("如果没有指定模板，将使用此规则随机生成指挥官")]
        public CommanderGenerationProfile randomCommanderProfile;

        [Header("Faction Settings")]
        public bool useCasterFaction = true;
        [Tooltip("如果 useCasterFaction 为 false，则使用此阵营")]
        public Faction specificFaction = Faction.Friend;

        [Header("Visuals")]
        public GameObject spawnVfxPrefab;

        public override void Execute(ActionContext ctx)
        {
            if (Battle.Instance == null || battalionDef == null) return;

            Vector2Int spawnPos = ctx.TargetPos;

            // 1. 检查位置是否有效且为空
            if (!Battle.Instance.IsValidMapPosition(spawnPos))
            {
                Debug.LogWarning("Summon failed: Invalid position.");
                return;
            }
            
            TileData tile = Battle.Instance.mapData[spawnPos.x, spawnPos.y];

            // 2. 确定阵营
            Faction finalFaction = specificFaction;
            if (useCasterFaction && ctx.User != null)
            {
                finalFaction = ctx.User.faction;
            }

            // 3. 创建 Descriptor
            BattalionDescriptor desc = new BattalionDescriptor
            {
                Definition = battalionDef,
                faction = finalFaction,
                isVisible = battalionDef.defaultVisibility, 
                
                maxSolider = battalionDef.defaultMaxSolider,
                maxMorale = battalionDef.defaultMaxMorale,
                
                currentSoliders = battalionDef.defaultMaxSolider,
                currentMorale = battalionDef.defaultMaxMorale,
                currentExp = 0,
                
                placed = false
            };

            // 4. 生成指挥官
            if (commanderTemplate != null)
            {
                desc.battalionCommander = commanderTemplate.CreateInstance(CommanderRegistry.Instance.GenerateID());
            }
            else
            {
                desc.battalionCommander = CommanderRegistry.Instance.GenerateCommander(randomCommanderProfile);
            }

            if (desc.battalionCommander != null)
            {
                desc.maxSolider += desc.battalionCommander.GetMaxSoldiersBonus();
                desc.maxMorale += desc.battalionCommander.GetMaxMoraleBonus();
                desc.actionChance += desc.battalionCommander.GetActionChanceBonus();
                
                desc.currentSoliders = desc.maxSolider;
                desc.currentMorale = desc.maxMorale;
            }
            
            // 6. 注册到战场
            Unit summonedUnit = Battle.Instance.RegisterUnitToBattle(desc, spawnPos);

            if (summonedUnit != null)
            {
                Debug.Log($"Summoned {summonedUnit.name} at {spawnPos}");
                summonedUnit.OnEnterNewTile(spawnPos);
                // 播放特效
                if (spawnVfxPrefab != null)
                {
                    // 转换到世界坐标
                    Vector3 worldPos = Battle.Instance.MapToWorld(spawnPos);
                    GameObject.Instantiate(spawnVfxPrefab, worldPos, Quaternion.identity);
                }
                
            }
        }
    }
}