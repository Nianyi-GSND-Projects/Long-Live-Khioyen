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
        
        [Header("Animation Settings")]
        [Tooltip("动作表现的时间间隔（秒）")]
        public float actionAnimationDuration = 0.5f;
        
        [Tooltip("聚焦时的镜头距离")]
        public float focusCameraDistance = 8f;
        
        [Tooltip("摄像机在施法者与目标之间切换镜头的过渡时间")]
        public float cameraTransitionDuration = 0.15f;

        [Header("Default Parameter")]
        public int defaultMaxExp = 100;

        public int defaultMaxMorale = 1000;

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
        
        [Header("视觉效果")]
        [Tooltip("己方单位在不可见状态下的透明度 (0-1)")]
        [Range(0, 1)] public float invisibleAllyAlpha = 0.5f;
        
        [Header("视野范围")] // [新增]
        [Tooltip("部署阶段，每个部署格提供的视野范围")]
        public int deployZoneVisionRange = 2;
        [Tooltip("撤离点提供的历史视野范围")]
        public int extractionZoneVisionRange = 1;

        [Header("士气损耗")] 
        [Tooltip("每回合自动损耗的士气")]
        public int moraleConsumePreTurn = 20;
        [Tooltip("最大士气损耗减免百分比")]
        public int maxMoraleConsumptionBonusPercent = 30;
        [Tooltip("士气为0时，每回合溃散的士兵比例 (0-1)")]
        [Range(0f, 1f)]
        public float moraleBreakAttritionRate = 0.1f;
        
        [Header("Loot")]
        public FacilityDefinition droppedLootChestDefinition;
        [Header("Item Rarity Colors")]
        public Color commonColor = Color.white;
        public Color uncommonColor = Color.green;
        public Color rareColor = Color.blue;
        public Color epicColor = Color.magenta;
        public Color legendaryColor = new Color(1f, 0.5f, 0f); // Orange

        public Color GetRarityColor(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Common: return commonColor;
                case Rarity.Uncommon: return uncommonColor;
                case Rarity.Rare: return rareColor;
                case Rarity.Epic: return epicColor;
                case Rarity.Legendary: return legendaryColor;
                default: return commonColor;
            }
        }
    }
}