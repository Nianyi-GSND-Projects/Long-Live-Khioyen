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
        
        private Battle Battle => Battle.Instance;
        private BattleUi BattleUi => BattleUi.Instance;

        private void Start()
        {
            if (button != null)
            {
                button.onClick.AddListener(OnClick);
            }

            if (Battle != null)
            {
                Battle.OnStageChanged += RefreshState;
                Battle.OnPlayerTurnStarted += RefreshState;
                Battle.OnUnitPlaced += RefreshState;
            }
            
            RefreshState();
        }

        private void OnDestroy()
        {
            if (Battle != null)
            {
                Battle.OnStageChanged -= RefreshState;
                Battle.OnPlayerTurnStarted -= RefreshState;
                Battle.OnUnitPlaced -= RefreshState;
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
                    bool hasUnits = Battle.GetUnitsByFaction(Faction.Player).Count > 0;
                    SetButton("Battle", hasUnits);
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