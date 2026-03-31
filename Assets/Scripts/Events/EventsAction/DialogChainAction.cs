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
        
        private GameCharacter GetActiveCharacter(BattleEventContext ctx)
        {
            // 尝试从黑板和数据库中获取动态角色
            if (useBlackboardName && ctx != null && ctx.HasData(nameKey))
            {
                string dynamicName = ctx.GetData<string>(nameKey);
            
                if (CharacterDatabase.Instance != null)
                {
                    GameCharacter dynamicChar = CharacterDatabase.Instance.GetCharacter(dynamicName);
                    if (dynamicChar != null)
                    {
                        return dynamicChar; // 成功匹配到动态角色！
                    }
                }
            }
        
            // 如果未开启动态、缺少上下文、或者数据库中没找到，则回退到静态配置
            return character;
        }
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
        
        public Sprite GetDisplayPortrait(BattleEventContext ctx = null)
        {
            // 1. 强制覆写头像的优先级最高
            if (overridePortrait != null) return overridePortrait;

            // 2. 解析激活的角色的头像
            GameCharacter activeChar = GetActiveCharacter(ctx);
            return activeChar != null ? activeChar.portrait : null;
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