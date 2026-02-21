using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LongLiveKhioyen
{
    public class BattleResultUI : MonoBehaviour
    {
        public static BattleResultUI Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Button exitButton;
        [SerializeField] private GameObject inputBlocker; // 全屏遮罩，防止点击其他东西

        private void Awake()
        {
            Instance = this;
            Hide();
            
            if (exitButton != null)
            {
                exitButton.onClick.AddListener(OnExitClicked);
            }
        }

        public void Show(BattleResult result)
        {
            if (panel != null) panel.SetActive(true);
            if (inputBlocker != null) inputBlocker.SetActive(true);
    
            if (resultText != null)
            {
                resultText.text = result.Victory ? "VICTORY" : "DEFEAT";
                resultText.color = result.Victory ? Color.yellow : Color.red;
            }

            // TODO: 显示 result.Loot
        }

        private void OnExitClicked()
        {
            // 调用 Battle 的退出逻辑
            if (Battle.Instance != null)
            {
                Battle.Instance.ExitBattle();
            }
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            if (inputBlocker != null) inputBlocker.SetActive(false);
        }
    }
}