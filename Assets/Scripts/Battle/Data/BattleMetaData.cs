using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    

    public class BattleMetaData
    {
        #region PreBattle Parameter
        
        public Vector2 battlePosition;
        //战役发生地在大地图上的坐标
        
        public Vector2 encounterOrientation;
        //玩家进入战役时行军的方向向量

        public int difficulty;
        //动态难度系数

        public WorldData.EnvironmentParams envParams;  // 环境参数
        #endregion

        #region BattleSetting
        
        [Header("Battle Generation")]
        public bool useRandomBattle = true;
        public BattlePresetSO fixedBattlePreset;

        #endregion
        
        public void GenerateMetaData()
        {
        }
    }
    
}
