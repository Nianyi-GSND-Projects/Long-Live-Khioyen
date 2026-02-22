using UnityEngine;

namespace LongLiveKhioyen
{
    public enum ContextSourceType
    {
        UnitName,
        UnitID,
        UnitFaction
    }

    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Events/Actions/Set Flag From Context")]
    public class SetFlagFromContextAction : GameEventAction
    {
        public string outputKey;
        public ContextSourceType sourceType;

        public override void Execute()
        {
            if (BattleEventManager.Instance == null || BattleEventManager.Instance.CurrentContext == null) return;
            
            var ctx = BattleEventManager.Instance.CurrentContext;
            var evt = BattleEventManager.Instance.CurrentEvent;
            
            if (ctx.TriggerUnit == null || evt == null) return;

            object val = null;
            switch (sourceType)
            {
                case ContextSourceType.UnitName:
                    val = ctx.TriggerUnit.name; // 或者 unitDefinition.unitName
                    break;
                case ContextSourceType.UnitID:
                    val = ctx.TriggerUnit.InstanceId;
                    break;
                // ...
            }
            
            evt.SetData(outputKey, val);
        }
    }
}