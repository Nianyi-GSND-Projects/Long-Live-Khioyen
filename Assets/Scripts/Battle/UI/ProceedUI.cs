using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LongLiveKhioyen
{
    public class ProceedUI : MonoBehaviour
    {
        [Header("References")]
        public Button button;
        public TMP_Text buttonText;
        
        // 引用其他 UI 组件以便调用它们的逻辑
        // 也可以通过 BattleUi 单例访问，这里为了解耦建议直接引用或通过 BattleUi 访问
        private Battle Battle => Battle.Instance;
        private BattleUi BattleUi => BattleUi.Instance;

        private void Start()
        {
            if (button != null)
            {
                button.onClick.AddListener(OnClick);
            }

            // 监听状态变化
            // 假设 Battle 有 OnStageChanged 事件，如果没有，我们需要在 Update 中轮询或者添加事件
            // 建议在 Battle.cs 中添加 OnStageChanged 事件
            if (Battle != null)
            {
                Battle.OnStageChanged += RefreshState;
                Battle.OnPlayerTurnStarted += RefreshState;
            }
            
            RefreshState();
        }

        private void OnDestroy()
        {
            if (Battle != null)
            {
                Battle.OnStageChanged -= RefreshState;
                Battle.OnPlayerTurnStarted -= RefreshState;
            }
        }

        private void RefreshState()
        {
            if (Battle == null) return;

            switch (Battle.CurrentStage)
            {
                case Stage.Preparation:
                    SetButton("Deploy", true);
                    gameObject.SetActive(true);
                    break;

                case Stage.Arrangement:
                    SetButton("Battle", true);
                    gameObject.SetActive(true);
                    break;

                case Stage.Battle:
                    if (Battle.CurrentTurnState == TurnState.PlayerTurn)
                    {
                        SetButton("End Turn", true);
                        gameObject.SetActive(true);
                    }
                    else
                    {
                        gameObject.SetActive(false);
                    }
                    break;
            }
        }

        private void SetButton(string text, bool interactable)
        {
            if (buttonText != null) buttonText.text = text;
            if (button != null) button.interactable = interactable;
        }

        private void OnClick()
        {
            if (Battle == null) return;

            switch (Battle.CurrentStage)
            {
                case Stage.Preparation:
                    Battle.ProceedToNextStage(); 
                    break;

                case Stage.Arrangement:
                    Battle.ProceedToNextStage();
                    break;

                case Stage.Battle:
                    if (Battle.CurrentTurnState == TurnState.PlayerTurn)
                    {
                        Battle.EndPlayerTurn();
                        gameObject.SetActive(false);
                        return;
                    }
                    else
                    {
                        gameObject.SetActive(false);
                    }
                    break;
            }
            
            RefreshState();
        }
    }
}