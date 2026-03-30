using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace LongLiveKhioyen
{
    [System.Serializable]
    public class DialogData
    {
        [Header("Character Reference")]
        public GameCharacter character;

        [Header("Overrides (Optional)")]
        [Tooltip("If empty, use character's name")]
        public string overrideName;
        [Tooltip("If null, use character's portrait")]
        public Sprite overridePortrait;
        
        [Header("Dynamic (Battle Only)")]
        public bool useBlackboardName;
        public string nameKey;

        [Header("Content")]
        [TextArea(3, 10)] public string dialogText;
        
        // 辅助属性：获取最终显示的名字
        public string DisplayName => !string.IsNullOrEmpty(overrideName) ? overrideName : (character != null ? character.characterName : "Unknown");
        
        // 辅助属性：获取最终显示的头像
        public Sprite DisplayPortrait => overridePortrait != null ? overridePortrait : (character != null ? character.portrait : null);
        
        public string GetDisplayName(BattleEventContext ctx = null)
        {
            // 如果勾选了动态名字，且传来了有效的战斗上下文，且上下文中包含该 Key
            if (useBlackboardName && ctx != null && ctx.HasData(nameKey))
            {
                return ctx.GetData<string>(nameKey);
            }
            
            // 否则退回到常规的名称获取逻辑
            return !string.IsNullOrEmpty(overrideName) ? overrideName : (character != null ? character.characterName : "Unknown");
        }
    }

    [CreateAssetMenu(menuName = "Long Live Khioyen/Events/Actions/Dialog Chain")]
    public class DialogChainAction : GameEventAction
    {
        [Header("id")] public int id;
        [Header("Dialog Sequence")]
        public List<DialogData> dialogList = new List<DialogData>();
        

        public override void Execute()
        {
            if (EventDialogUI.Instance != null)
            {
                EventDialogUI.Instance.StartDialogChain(this, null);
            }
        }
        
        public override IEnumerator ExecuteCoroutine()
        {
            if (EventDialogUI.Instance != null)
            {
                EventDialogUI.Instance.StartDialogChain(this, null);
        
                // 等待直到 UI 关闭
                while (EventDialogUI.Instance.IsActive)
                {
                    yield return null;
                }
            }
        }
        
        public override void Execute(BattleEventContext ctx)
        {
            if (EventDialogUI.Instance != null)
            {
                // 将上下文传递给 UI，UI 内部渲染名字时就可以调用 dialogData.GetDisplayName(ctx)
                EventDialogUI.Instance.StartDialogChain(this, ctx);
            }
        }

        public override IEnumerator ExecuteCoroutine(BattleEventContext ctx)
        {
            if (EventDialogUI.Instance != null)
            {
                EventDialogUI.Instance.StartDialogChain(this, ctx);
        
                while (EventDialogUI.Instance.IsActive)
                {
                    yield return null;
                }
            }
        }
        
       
    }
}