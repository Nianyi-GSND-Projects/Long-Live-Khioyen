using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LongLiveKhioyen
{
    public class ArrangementUI : MonoBehaviour
    {
        
        Battle Battle => Battle.Instance;
        
        public LayoutGroup ArrangementLayoutGroup;
        
        #region Life cycle
		void OnEnable()
		{
            // UI 初始化逻辑
		}
        #endregion
       
		 #region UI

		 public void InitializeUi()
		 {
			 GenerateUi();
			 Debug.Log("Arrangement Modal Enabled");
			 Battle.SelectedBattalionDescriptor = null;
		 }
         
		void GenerateUi()
		{
			// 清理旧卡片
			List<Transform> children = new();
			for(int i = 0; i < ArrangementLayoutGroup.transform.childCount; ++i)
				children.Add(ArrangementLayoutGroup.transform.GetChild(i));
			foreach(var child in children)
				Destroy(child.gameObject);
  
            // 生成新卡片
			var cardTemplate = Resources.Load<GameObject>("Prefabs/Battle/UI/Battalion_Arrangement");
			foreach(var reserveTeam in Battle.playerReserveTeam)
			{
				var card = Instantiate(cardTemplate).GetComponent<BattalionArrangementUi>();
				
                // [关键] 调用 Setup
                card.Setup(reserveTeam);
                
				card.transform.SetParent(ArrangementLayoutGroup.transform, false);
  
				card.onSelected += OnBattalionCardSelected;
				card.onHovered += OnBattalionCardHovered;
				card.onUnhovered += OnBattalionCardUnhovered;
			}
			ArrangementLayoutGroup.CalculateLayoutInputHorizontal();
		}
  
		void OnBattalionCardSelected(BattalionArrangementUi card)
		{
            // 仅仅通知 Battle 选中了哪个预备队
            // 具体的放置逻辑由 BattleInputController -> Battle.HandleGridInput 处理
			if (!card.battalionDescriptor.placed)
			{
				Battle.SelectedBattalionDescriptor = card.battalionDescriptor;
				Battle.IsReserveTeamSelected = true;
			}
		}
  
		void OnBattalionCardHovered(BattalionArrangementUi card)
		{
			_hoveredBattalionDescriptor = card.battalionDescriptor;
		}
  
		void OnBattalionCardUnhovered(BattalionArrangementUi card)
		{
            _hoveredBattalionDescriptor = null;
		}
		 #endregion

		#region Selection
		BattalionDescriptor  _hoveredBattalionDescriptor;
		#endregion
    }
}