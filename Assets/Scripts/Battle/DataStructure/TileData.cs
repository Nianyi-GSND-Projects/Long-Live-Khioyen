using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public enum ZOCState
    {
        Neutral,
        PlayerControlled,
        EnemyControlled
    }
    
    [System.Serializable]
    public class TileData
    {
        public Battalion Battalion;
        public Facility Facility;
        public List<TileEffect> Effects = new List<TileEffect>();
        public bool IsEmpty => Battalion == null && Facility == null;
        
        public bool isExtractionPoint = false;
        public GameObject TileVFX;

        #region ZOC
        
        public int PlayerZOC = 0;
        public int EnemyZOC = 0;
    
        public ZOCState GetZOCState()
        {
            if (PlayerZOC > EnemyZOC) return ZOCState.PlayerControlled;
            if (EnemyZOC > PlayerZOC) return ZOCState.EnemyControlled;
            return ZOCState.Neutral;
        }
        

        #endregion
    }
}
