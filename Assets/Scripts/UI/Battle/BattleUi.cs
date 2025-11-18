using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LongLiveKhioyen
{
    public class BattleUi : MonoBehaviour
    {
        #region Life cycle
        void Awake()
        {
            Battle.Instance.onInitialized += Initialize;
        }

        void Initialize()
        {
            battle.OnPlayerTurnStarted += HandlePlayerTurnStart;
            battle.OnPlayerTurnEnded += HandlePlayerTurnEnd;
            battle.OnActionSelectionStarted += HandleActionSelectionStart;
            battle.OnActionSelectionEnded += HandleActionSelectionEnd;
        }
        
        private void OnDestroy()
        {

            if (battle != null)
            {
                battle.OnPlayerTurnStarted -= HandlePlayerTurnStart;
                battle.OnPlayerTurnEnded -= HandlePlayerTurnEnd;
                battle.OnActionSelectionStarted -= HandleActionSelectionStart;
                battle.OnActionSelectionEnded -= HandleActionSelectionEnd;
            }
        }
        #endregion
        
        #region Event Handler
        private void HandlePlayerTurnStart()
        {
            Debug.Log("BattleUI 收到信号：玩家回合开始，显示UI。");
            OpenPanel(playerTurnUI);
        }

        private void HandlePlayerTurnEnd()
        {
            Debug.Log("BattleUI 收到信号：玩家回合结束，关闭UI。");
            ClosePanel(playerTurnUI);
        }
        
        private void HandleActionSelectionStart()
        {
            Debug.Log("BattleUI 收到信号：为选中单位显示行动面板");
            OpenPanel(actionSelectionPanel);
        }
        
        private void HandleActionSelectionEnd()
        {
            Debug.Log("BattleUI 收到信号：为选中单位关闭行动面板");
            ClosePanel(actionSelectionPanel);
        }
        #endregion
        
        #region General
        public Battle battle;

        public void OpenPauseMenu()
        {
            GameInstance.Instance.OpenPauseMenu();
        }
        #endregion
        
        #region BottomPanel
        [Header("Bottom Panel")]
        public CanvasGroup ArrangementPanel;
        
        [Header("Bottom Button")]
        public GameObject ArrangementButtonPrefab;
        public RectTransform contentContainer;
        
        #endregion
        
        
        public CanvasGroup playerTurnUI;
        public CanvasGroup actionSelectionPanel; 
        
        #region StageManagement
        public Button toArrangementButton;
        public Button toBattleButton;
        public void PreparationToArrangement()
        {
            toArrangementButton.gameObject.SetActive(false);
            OpenPanel(ArrangementPanel);
            toBattleButton.gameObject.SetActive(true);
            EnterArrangementMode();
        }

        public void ArrangementToBattle()
        {
            toBattleButton.gameObject.SetActive(false);
            ClosePanel(ArrangementPanel);
            ExitArrangementMode();
            EnterBattleMode();
        }
        public void EnablePlayerUI(bool enable)
        {
            if(enable) OpenPanel(playerTurnUI);
            else ClosePanel(playerTurnUI);
        }
        
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
