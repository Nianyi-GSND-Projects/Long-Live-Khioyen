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
        [NonSerialized] public ReserveTeam reserveTeam;
        
        LocalizedString localizedBattalionName;
        public Text buttonText;
        
        CanvasGroup group;
        [SerializeField] Button button;
        [SerializeField] TMP_Text text;
        [SerializeField] Image image;
        
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
          //  localizedBattalionName.TableEntryReference = battalionDefinition.battalionId;
           // localizedBattalionName.RefreshString();
           // image.sprite = battalionDefinition.figure;
        }
        
        protected void OnDestroy()
        {
           
        }
        public void OnPointerEnter(PointerEventData eventData) => onHovered?.Invoke(this);
        public void OnPointerExit(PointerEventData eventData) => onUnhovered?.Invoke(this);
        public void Setup(ReserveTeam reserveTeam)
        {
            buttonText.text = reserveTeam.battalionDefinition.battalionId;
            Debug.Log("预备队已生成");
        }
    }
}
