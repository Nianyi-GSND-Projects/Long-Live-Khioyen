using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public partial class Battle
    {
       
        [Header("Test Config")]
        public FacilityDefinition testFacilityDefinition;
        public BattalionDefinition defaultReserveTeamDefinition;
        public BattalionDefinition defaultEnemyDefinition;
        public int testPlayerReserveTeamCount;
        
        private void GenerateTestData()
        {
            data = new BattleMetaData()
            {
                battleName = "Battle of Test",
                battleId = 0,
                battleTime = 0,
                battleType = BattleType.Encounter,
                battleSize = new Vector2Int(10, 10),
                battlePosition = new Vector2Int(0, 0),
                encounterOrientation = new Vector2Int(0, 0),
                battleGoal = BattleGoal.Annihilate,
                enemyCount = 4
            };
        }
        
        private void GenerateTestArmyData()
        {
            armyStatus.battalionStatuses.Clear();
			
            for (int i = 0; i < testPlayerReserveTeamCount; i++)
            {
                BattalionStatus battalionStatus = new BattalionStatus()
                {
                    battalionId = i,
                    battalionName = "TestBattalion" + i,
                    battalionCommander = CommanderRegistry.Instance.GenerateCommander(CommanderGenerationProfile.Default),
                    battalionDefinition = defaultReserveTeamDefinition
                };
				
                battalionStatus.currentSolider = battalionStatus.MaxSolider;
                battalionStatus.currentMorale = battalionStatus.MaxMorale;
                battalionStatus.currentExp = battalionStatus.MaxExp;
				
                armyStatus.battalionStatuses.Add(battalionStatus);
                battalionStatus.battalionCommander.isAssigned = true;
            }

        }
    }
}
