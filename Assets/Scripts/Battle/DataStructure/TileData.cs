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
        
        public (int player, int enemy) GetZOCRadiation()
        {
            int p = 0;
            int e = 0;

            void AddRadiation(Unit unit)
            {
                if (unit != null && unit.IsVisible)
                {
                    if (unit.faction == Faction.Player || unit.faction == Faction.Friend) p += (int)unit.GetStat(StatType.ZocPower);
                    else if (unit.faction == Faction.Enemy) e += (int)unit.GetStat(StatType.ZocPower);
                }
            }
            AddRadiation(Battalion);
            AddRadiation(Facility);
            return (p, e);
        }

        #endregion
    }
}
