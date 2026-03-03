// D:/WorkPlace/GSND/Khioyen/Assets/Scripts/Battle/Data/BattleParam.cs

using UnityEngine;

namespace LongLiveKhioyen
{
    /// <summary>
    /// 用于定义指挥官五维属性到部队五大属性的加成系数
    /// </summary>
    [System.Serializable]
    public class CommanderStatScaling
    {
        [Tooltip("智力对该项属性的加成系数")]
        public float zhiFactor;
        [Tooltip("信义对该项属性的加成系数")]
        public float xinFactor;
        [Tooltip("仁德对该项属性的加成系数")]
        public float renFactor;
        [Tooltip("勇武对该项属性的加成系数")]
        public float yongFactor;
        [Tooltip("严明对该项属性的加成系数")]
        public float yanFactor;
    }

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

        [Header("Default Parameter")]
        public int defaultMaxExp = 100;

        public int defaultMaxMorale = 100;

        public int defaultMaxSolider = 1000;

        public float defaultStrategySuccessRate = 0.5f;
        [Tooltip("每拥有多少士兵，部队总体的攻击力会增加等于其面板攻击的数值？")]
        public float defaultSoliderAmountForOnePower = 100;

        [Header("将领属性 -> 部队属性转换系数")]
        [Tooltip("部队攻击力 = 智*Zhi + 信*Xin + 仁*Ren + 勇*Yong + 严*Yan")]
        public CommanderStatScaling attackScaling;
        [Tooltip("部队防御力 = 智*Zhi + 信*Xin + 仁*Ren + 勇*Yong + 严*Yan")]
        public CommanderStatScaling defenseScaling;
        [Tooltip("部队机动力 = 智*Zhi + 信*Xin + 仁*Ren + 勇*Yong + 严*Yan")]
        public CommanderStatScaling mobilityScaling;
        [Tooltip("部队策略值 = 智*Zhi + 信*Xin + 仁*Ren + 勇*Yong + 严*Yan")]
        public CommanderStatScaling strategyScaling;
        [Tooltip("部队纪律值 = 智*Zhi + 信*Xin + 仁*Ren + 勇*Yong + 严*Yan")]
        public CommanderStatScaling disciplineScaling;

        [Header("部队属性 -> 战斗效果折算率")]
        [Tooltip("每多少点【机动力】折算为1点战场移动力 (Movement)")]
        public float mobilityPerMovement = 10f;

        [Tooltip("每1点【攻击】造成多少点基础伤害")]
        public float damagePerAttack = 1f;

        [Tooltip("每1点【防御】提供多少伤害抵抗系数")]
        public float damageResistancePerDefense = 0.05f;

        [Tooltip("每1点【策略】值提供多少控制力")]
        public float zocPowerPerStrategy = 0.1f;
        [Tooltip("每1点【策略】值增加多少计策成功率")]
        public float schemeSuccessRatePerStrategy = 0.01f;

        [Tooltip("每1点【纪律】值增加多少建设效率")]
        public float constructionRatePerDiscipline = 0.01f;
        [Tooltip("每1点【纪律】值增加多少战后伤兵恢复率")]
        public float recoveryRatePerDiscipline = 0.01f;
    }
}