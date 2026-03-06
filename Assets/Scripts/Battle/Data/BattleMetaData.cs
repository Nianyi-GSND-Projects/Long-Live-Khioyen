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

        public int difficulity;
        //动态难度系数
        #endregion

        #region BattleSetting
        
        [Header("Battle Generation")]
        public bool useRandomBattle = true;
        public BattlePresetSO fixedBattlePreset;

        #endregion
        
        public void GenerateMetaData()
        {
            //从GameInstance中取得需要的数据
        }
        //TODO MetaData中应该包含一系列更具体指导地图生成的数据，例如：如果有预设地图，那预设地图的索引是多少；如果没有，则根据一系列参数随机生成地图。
    }
    
}
