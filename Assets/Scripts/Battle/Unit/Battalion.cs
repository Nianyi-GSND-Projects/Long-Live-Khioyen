using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public class Battalion : Unit<BattalionDefinition>
    {
        public List<inBattleItem> inventory;
        public GameCommander battalionCommander;
        
        public int currentSoliders;
        public int currentMurale;
        public int currentTraining;
        
        public int currentMovement;

        public Battalion()
        {
            InstanceId = 0;
            inventory = new List<inBattleItem>();
        }

        public override void TakeDamage(int damage)
        {
            currentSoliders -= damage;
            Debug.Log($"Battalion {InstanceId} take {damage} damage, current soliders: {currentSoliders}");
        }

        public override float GetPower()
        {
            return Definition.defaultPower * currentSoliders*1.0f / Definition.defaultMaxSolider;
            //Todo 将领系数影响
        }

        public override void ApplyBuff(BuffDescriptor buffDescriptor)
        {
            if (buffDescriptor.definition.unitType == BuffUnitType.Battalion ||
                buffDescriptor.definition.unitType == BuffUnitType.Both)
            {
                //处理逻辑
                Buff newBuff = new Buff()
                {
                    descriptor = buffDescriptor,
                    currentDuration = buffDescriptor.defaultDuration
                };
                
                buffs.Add(newBuff);
                
            }
        }
    }
    
    public class BattalionDescriptor
    {
        public int armyId;//部队在军队列表中的索引
        public Faction faction;
        public Vector2Int position;
        public BattalionDefinition Definition;
        public GameCommander battalionCommander;
        public int maxSolider;
        public int maxMorale;
        public int maxTraining;
        public int currentSoliders;
        public int currentMurale;
        public int currentTraining;
        
        
        public bool placed = false;
    }
}
