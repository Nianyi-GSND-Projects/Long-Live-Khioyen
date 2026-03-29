using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LongLiveKhioyen
{
    public class BattleUi : MonoBehaviour
    {
        #region Life cycle
        void Awake()
        {
            Instance = this;
            if (Battle.Instance != null)
                Battle.Instance.onInitialized += Initialize;
        }
        public static BattleUi Instance { get; private set; }
        void Initialize()
        {
            battle.OnPlayerTurnStarted += HandlePlayerTurnStart;
            battle.OnPlayerTurnEnded += HandlePlayerTurnEnd;
            battle.OnActionSelectionStarted += HandleActionSelectionStart;
            battle.OnActionSelectionEnded += HandleActionSelectionEnd;
            battle.OnUnitSelectionChanged += HandleUnitSelectionChanged;
            battle.OnReserveTeamSelectionChanged += HandleReserveSelectionChanged;
            battle.OnAmbiguousSelectionStarted += HandleAmbiguousSelectionStart;
            battle.OnAmbiguousSelectionEnded += HandleAmbiguousSelectionEnd;
            battle.OnStageChanged += HandleStageChanged;
            battle.BattleStart += HandleBattleStart;
        }
        
        private void OnDestroy()
        {

            if (battle != null)
            {
                battle.OnPlayerTurnStarted -= HandlePlayerTurnStart;
                battle.OnPlayerTurnEnded -= HandlePlayerTurnEnd;
                battle.OnActionSelectionStarted -= HandleActionSelectionStart;
                battle.OnActionSelectionEnded -= HandleActionSelectionEnd;
                battle.OnUnitSelectionChanged -= HandleUnitSelectionChanged;
                battle.OnReserveTeamSelectionChanged -= HandleReserveSelectionChanged;
                battle.OnAmbiguousSelectionStarted -= HandleAmbiguousSelectionStart;
                battle.OnAmbiguousSelectionEnded -= HandleAmbiguousSelectionEnd;
                battle.OnStageChanged -= HandleStageChanged;
                battle.BattleStart -= HandleBattleStart;
            }
        }
        #endregion
        
        #region Event Handler
        private void HandlePlayerTurnStart()
        {
            Debug.Log("BattleUI 收到信号：玩家回合开始，显示UI。");
        }
        
        private void HandleBattleStart()
        {
            Debug.Log("TitleUI Set");
            Debug.Log("BattleUI 收到信号：战斗开始，显示UI。");
            if (battleTitlePanel != null && Battle.Instance.levelPreset != null)
            {
                
                battleTitlePanel.ShowTitle(Battle.Instance.levelPreset.levelNameZH, Battle.Instance.levelPreset.levelNameEN);
            }
        }
        
        private void HandlePlayerTurnEnd()
        {
            Debug.Log("BattleUI 收到信号：玩家回合结束，关闭UI。");
        }
        
        private void HandleActionSelectionStart()
        {
            Debug.Log("BattleUI 收到信号：为选中单位显示行动面板");
            //OpenPanel(actionSelectionPanel);
            if (actionMenu != null && Battle.Instance.SelectedUnit != null)
            {
                actionMenu.Show(Battle.Instance.SelectedUnit);
            }
        }
        
        private void HandleActionSelectionEnd()
        {
            Debug.Log("BattleUI 收到信号：为选中单位关闭行动面板");
            //ClosePanel(actionSelectionPanel);
            if (actionMenu != null)
            {
                actionMenu.Hide();
            }
        }
        private void HandleStageChanged()
        {
            switch (battle.CurrentStage)
            {
                case Stage.Arrangement:
                    PreparationToArrangement();
                    break;
            
                case Stage.Battle:
                    ArrangementToBattle();
                    break;
                
                // ...
            }
        }
        
        private void HandleUnitSelectionChanged(Unit unit)
        {
            if (unit == null)
            {
                if(!battle.IsReserveTeamSelected) 
                    ClosePanel(unitInfoPanel.canvasGroup);
            }
            else
            {
                if (battle.CurrentActionStage == PlayerActionStage.SelectingAmbiguousTarget) 
                    return;
                unitInfoPanel.UpdateUI(unit);
                OpenPanel(unitInfoPanel.canvasGroup);
            }
        }
        
        private void HandleReserveSelectionChanged(BattalionDescriptor desc)
        {
            if (desc == null)
            {
                if(!battle.IsUnitSelected)
                    ClosePanel(unitInfoPanel.canvasGroup);
            }
            else
            {
                unitInfoPanel.UpdateUI(desc);
                OpenPanel(unitInfoPanel.canvasGroup);
            }
        }
        
        public void PreparationToArrangement()
        {
            // toArrangementButton.gameObject.SetActive(false); // [移除]
            OpenPanel(ArrangementPanel);
            // toBattleButton.gameObject.SetActive(true); // [移除]
        
            EnterArrangementMode();
        
            // [新增] 初始化子 UI
            if (arrangementUI != null)
            {
                arrangementUI.InitializeUi();
            }
        }

        public void ArrangementToBattle()
        {
            // toBattleButton.gameObject.SetActive(false); // [移除]
            ClosePanel(ArrangementPanel);
            ExitArrangementMode();
            EnterBattleMode();
        }
        
        private void HandleAmbiguousSelectionStart(List<Unit> candidates)
        {
            Debug.Log("BattleUI: 显示多重选择列表");
            if (candidates.Count > 0)
            {
                // 获取面板的 RectTransform
                RectTransform panelRect = ambiguousSelectionPanel.GetComponent<RectTransform>();
                
                float screenWidth = Screen.width;
                float screenHeight = Screen.height;
                Vector2 newPivot = new Vector2(0, 1);
                
                // A. 设置 Pivot 为左上角 (0, 1)
                // 这样当我们把 position 设为屏幕某点时，UI 会向右、向下延伸
                
                // (可选) 建议把 Anchor 设为左下角或其他固定值，防止父级布局拉伸影响
                // 如果你的 Panel 是全屏 Canvas 的直接子物体，这行通常不需要，或者保持原样即可
                // panelRect.anchorMin = Vector2.zero; 
                // panelRect.anchorMax = Vector2.zero;

                // B. 获取目标格子的世界坐标
                // 因为所有 candidate 都在同一个格子里，取第一个的位置即可
                Vector3 worldPos = Battle.Instance.MapToWorld(candidates[0].position);

                // C. 转换为屏幕坐标
                // Camera.main 必须是你的主渲染相机
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                
                if (screenPos.x > screenWidth * 0.8f) newPivot.x = 1;

                // 如果点击位置在下半屏，改为左下角/右下角 Pivot (菜单向上展开)
                if (screenPos.y < screenHeight * 0.2f) newPivot.y = 0;
                // D. 应用坐标
                // 注意：WorldToScreenPoint 返回的 z 是深度，对于 Screen Space - Overlay 的 Canvas，通常不需要 Z
                // 但直接赋值 Vector3 也是安全的，Unity 会忽略 Z 或处理它
                panelRect.pivot = newPivot;
                panelRect.position = screenPos;
            }
            
            foreach (Transform child in ambiguousListContainer)
            {
                Destroy(child.gameObject);
            }

            // 2. 生成新按钮
            foreach (var unit in candidates)
            {
                GameObject btnObj = Instantiate(ambiguousButtonPrefab, ambiguousListContainer);
                
                // 设置按钮文本
                TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    if (unit is Battalion bat)
                        btnText.text = $"部队: {bat.Definition.unitName}";
                    else if (unit is Facility fac)
                        btnText.text = $"设施: {fac.Definition.unitName}";
                    else
                        btnText.text = "未知单位";
                }

                // 绑定点击事件
                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners(); 
                    
                    // 添加新的监听器
                    btn.onClick.AddListener(() =>
                    {
                        // 调用 Battle 的解决逻辑
                        Battle.Instance.ResolveAmbiguousSelection(unit);
                    });
                }
            }

            // 3. 打开面板
            OpenPanel(ambiguousSelectionPanel);
        }

        // [新增] 处理多重目标选择结束
        private void HandleAmbiguousSelectionEnd()
        {
            Debug.Log("BattleUI: 关闭多重选择列表");
            ClosePanel(ambiguousSelectionPanel);
        }
        
        #endregion
        
        #region General
        public Battle battle;

        public void OpenPauseMenu()
        {
            GameInstance.Instance.OpenPauseMenu();
        }
        #endregion
        
        #region UnitInfoPanel
        
        public UnitInfoPanel unitInfoPanel;
        #endregion
        
        [Header("Ambiguous Selection")] // [新增]
        public CanvasGroup ambiguousSelectionPanel; // 挂载 CanvasGroup 的选择面板
        public Transform ambiguousListContainer;    // 放置按钮的 Grid/Vertical Layout Group
        public GameObject ambiguousButtonPrefab;    // 按钮预制体
        
        
        
        #region BottomPanel
        [Header("Bottom Panel")]
        public CanvasGroup ArrangementPanel;
        
        [Header("Bottom Button")]
        public GameObject ArrangementButtonPrefab;
        public RectTransform contentContainer;
        
        [Header("Sub Views")]
        public ArrangementUI arrangementUI;
        #endregion
        
        public ConstructionUI constructionUI;
        public BattleTitlePanel battleTitlePanel;
        public ActionMenuUI actionMenu; 
        
        #region StageManagement
        
        public void ClosePanel(CanvasGroup canvasGroup)
        {
            canvasGroup.interactable = false;
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false; 
        }
        
        public void OpenPanel(CanvasGroup canvasGroup)
        {
            canvasGroup.interactable = true;
            canvasGroup.alpha = 1;
            canvasGroup.blocksRaycasts = true; 
        }

        public void EnterArrangementMode()
        {
            battle.IsInArrangementStage = true;
        }
        public void EnterBattleMode()
        {
            battle.IsInBattleStage = true;
        }
        public void ExitArrangementMode()
        {
            battle.IsInArrangementStage = false;
        }
        
        #endregion
        
        
    }
}
