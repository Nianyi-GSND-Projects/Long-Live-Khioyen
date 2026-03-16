using UnityEngine;
using System;

namespace LongLiveKhioyen
{
    public enum ComparisonOperator
    {
        GreaterThan,        // >
        LessThan,           // <
        GreaterThanOrEqual, // >=
        LessThanOrEqual,    // <=
        Equal,              // ==
        NotEqual            // !=
    }
    
    public enum ValueSourceType
    {
        Constant, 
        
        // --- 指挥官属性 ---
        Commander_Zhi,      // 智
        Commander_Xin,      // 信
        Commander_Ren,      // 仁
        Commander_Yong,     // 勇
        Commander_Yan,      // 严
        
        // --- 单位属性 ---
        Unit_CurrentSoldier,
        Unit_CurrentMorale,
        Unit_Movement,
        
        // --- 全局/战斗属性 (可扩展) ---
        Battle_TurnCount,
        Unit_IsOnExtractionPoint,
        Unit_HasNotMoved
    }
    public enum ConditionCategory
    {
        NumericalComparison,
        CheckExternalTag
    }
    
    [Serializable]
    public class ConditionOperand
    {
        public ValueSourceType sourceType;
        public float constantValue;

        // 核心方法：根据上下文获取实际数值
        // contextUser: 施法者/行动发起者
        // contextTarget: 目标 (如果是展示条件，target 可能为 null)
        public float GetValue(Unit contextUser, Unit contextTarget)
        {
            switch (sourceType)
            {
                case ValueSourceType.Constant:
                    return constantValue;

                // --- 指挥官属性 (需要强转检查) ---
                case ValueSourceType.Commander_Zhi:
                    return GetCommanderStat(contextUser, c => c.Zhi);
                case ValueSourceType.Commander_Xin:
                    return GetCommanderStat(contextUser, c => c.Xin);
                case ValueSourceType.Commander_Ren:
                    return GetCommanderStat(contextUser, c => c.Ren);
                case ValueSourceType.Commander_Yong:
                    return GetCommanderStat(contextUser, c => c.Yong);
                case ValueSourceType.Commander_Yan:
                    return GetCommanderStat(contextUser, c => c.Yan);

                // --- 单位属性 ---
                case ValueSourceType.Unit_CurrentSoldier:
                    return contextUser is Battalion b1 ? b1.currentSoliders : 0;
                case ValueSourceType.Unit_CurrentMorale:
                    return contextUser is Battalion b2 ? b2.currentMorale : 0;
                case ValueSourceType.Unit_Movement:
                    return contextUser is Battalion b3 ? b3.currentMovement : 0;

                // --- 全局属性 ---
                case ValueSourceType.Battle_TurnCount:
                    return Battle.Instance != null ? Battle.Instance.TurnCount : 0;
                
                case ValueSourceType.Unit_IsOnExtractionPoint:
                    if (Battle.Instance != null && contextUser != null)
                    {
                        var tile = Battle.Instance.mapData[contextUser.position.x, contextUser.position.y];
                        return tile.isExtractionPoint ? 1f : 0f;
                    }
                    return 0f;
                
                case ValueSourceType.Unit_HasNotMoved:
                    return !contextUser.hasMovedThisTurn ? 1f : 0f;
                    
                
                default:
                    return 0;
            }
        }
        
        private float GetCommanderStat(Unit unit, Func<GameCommander, float> selector)
        {
            if (unit is Battalion bat && bat.battalionCommander != null)
            {
                return selector(bat.battalionCommander);
            }
            return 0;
        }
    }
    
    [Serializable]
    public class ActionCondition
    {
        public ConditionCategory category = ConditionCategory.NumericalComparison;
        
        public ConditionOperand operandA;
        public ComparisonOperator compareOp;
        public ConditionOperand operandB;
        [Tooltip("要检查的外部 Tag")]
        public string targetTag;
        
        // 判定逻辑
        public bool Evaluate(Unit user, Unit target)
        {
            switch (category)
            {
                case ConditionCategory.NumericalComparison:
                    return EvaluateNumerical(user, target);
                
                case ConditionCategory.CheckExternalTag:
                    return EvaluateTag();
        
                default:
                    return false;
            }
        }

        private bool EvaluateNumerical(Unit user, Unit target)
        {
            if (operandA == null || operandB == null) return false;

            float valA = operandA.GetValue(user,target);
            float valB = operandB.GetValue(user, target);

            switch (compareOp)
            {
                case ComparisonOperator.GreaterThan:        return valA > valB;
                case ComparisonOperator.LessThan:           return valA < valB;
                case ComparisonOperator.GreaterThanOrEqual: return valA >= valB;
                case ComparisonOperator.LessThanOrEqual:    return valA <= valB;
                case ComparisonOperator.Equal:              return Mathf.Abs(valA - valB) < 0.001f;
                case ComparisonOperator.NotEqual:           return Mathf.Abs(valA - valB) > 0.001f;
                default: return false;
            }
        }

        private bool EvaluateTag()
        {
            if (string.IsNullOrEmpty(targetTag)) return true;
            if (PolisData.Main != null)
            {
                if (PolisData.Main.HasBuildingWithTag(targetTag)) return true;
            }
            return false; 
        }
    }
}