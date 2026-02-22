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
        
        public string GetDisplayName(BattleEventDefinition evt)
        {
            if (useBlackboardName && evt != null && evt.HasData(nameKey))
            {
                return evt.GetData<string>(nameKey);
            }
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
                EventDialogUI.Instance.StartDialogChain(this);
            }
        }
        
        public override IEnumerator ExecuteCoroutine()
        {
            if (EventDialogUI.Instance != null)
            {
                EventDialogUI.Instance.StartDialogChain(this);
        
                // 等待直到 UI 关闭
                while (EventDialogUI.Instance.IsActive)
                {
                    yield return null;
                }
            }
        }
        
       
    }
}