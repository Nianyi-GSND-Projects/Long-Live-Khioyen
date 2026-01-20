using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(fileName = "Battle Parameter", menuName = "Long Live Khioyen/Battle/Battle Parameter")]
    public class BattleParam : ScriptableObject
    {
        #region Singleton
        private static BattleParam _instance;

        public static BattleParam Instance
        {
            get
            {
                if (_instance == null)
                {
                    BattleParam orignal = Resources.Load<BattleParam>("Data/BattleParam");
                    if (orignal != null)
                    {
                        _instance = Instantiate(orignal);
                    }
                    else
                    {
                        Debug.LogError("Cannot find battle parameter asset.");
                    }
                }
  
                return _instance;
            }
        }

        #endregion

        [Header("Test Parameter")]
        //

        [Header("Default Parameter")]
        public int defaultMaxExp;
        
        [Header("Army Parameter")]
        public float solidersPerZhi;
        public float solidersPerXin;
        public float solidersPerRen;
        public float solidersPerYong;
        public float solidersPerYan;
        
        public float moralePerZhi;
        public float moralePerXin;
        public float moralePerRen;
        public float moralePerYong;
        public float moralePerYan;
        
    }
}
