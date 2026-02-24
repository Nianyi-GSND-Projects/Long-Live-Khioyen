using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using System;
using TMPro;

namespace LongLiveKhioyen

{
    
    public class BattalionArrangementUi : MonoBehaviour,IPointerEnterHandler, IPointerExitHandler
    {
        Battle Battle => Battle.Instance;
        [NonSerialized] public BattalionDescriptor battalionDescriptor;
        
        LocalizedString localizedBattalionName;
        
        CanvasGroup group;
        [SerializeField] Button button;
        [SerializeField] private TMP_Text unitNameText;
        [SerializeField] private TMP_Text commanderNameText;
        [SerializeField] Image unitIcon;
        
        public Action<BattalionArrangementUi> onSelected, onHovered, onUnhovered;
        
        protected void Awake()
        {
            group = GetComponent<CanvasGroup>();
         //   localizedBattalionName = new("Building Names", "");
          //  localizedBattalionName.StringChanged += s => text.text = s;

            button.onClick.AddListener(() => onSelected?.Invoke(this));
        }
        
        protected void Start()
        {
          //  localizedBattalionName.TableEntryReference = battalionDefinition.armyId;
           // localizedBattalionName.RefreshString();
           // image.sprite = battalionDefinition.figure;
        }
        
        protected void OnDestroy()
        {
           
        }
        public void OnPointerEnter(PointerEventData eventData) => onHovered?.Invoke(this);
        public void OnPointerExit(PointerEventData eventData) => onUnhovered?.Invoke(this);

        public void Setup(BattalionDescriptor descriptor)
        {
            this.battalionDescriptor = descriptor;
            
            if (descriptor == null || descriptor.Definition == null) return;
            
            if (unitNameText != null)
                unitNameText.text = descriptor.Definition.unitName;

            // 2. 设置指挥官名
            if (commanderNameText != null)
            {
                if (descriptor.battalionCommander != null)
                {
                    commanderNameText.text = descriptor.battalionCommander.commanderName;
                }
                else
                {
                    commanderNameText.text = "无指挥官"; // 或空字符串
                }
            }

            // 3. 设置图标
            if (unitIcon != null)
                unitIcon.sprite = descriptor.Definition.figure;
        }
    }
}
