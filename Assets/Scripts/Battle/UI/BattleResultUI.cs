using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LongLiveKhioyen
{
    public class BattleResultUI : MonoBehaviour
    {
        public static BattleResultUI Instance { get; private set; }
        
        [Header("Pages")]
        public GameObject page1_ArmyStatus;
        public GameObject page2_Loot;
        
        [Header("Components - Page 1")]
        public TextMeshProUGUI titleText;
        public Transform armyListContainer;
        public GameObject unitEntryPrefab;
        
        [Header("Components - Page 2")]
        public Transform lootListContainer;
        public GameObject lootEntryPrefab;
        public ItemTooltip itemTooltip;

        [Header("Navigation")]
        public Button actionButton;
        public TextMeshProUGUI actionButtonText;
        
        private BattleResult _result;
        private int _currentPage = 1;
        
        [SerializeField] private GameObject inputBlocker; // 全屏遮罩，防止点击其他东西

        private void Awake()
        {
            Instance = this;
            gameObject.SetActive(false);
            actionButton.onClick.AddListener(OnActionButtonClicked);
        }

        public void Show(BattleResult result)
        {
            _result = result;
            gameObject.SetActive(true);
            if (inputBlocker != null) inputBlocker.SetActive(true);
            titleText.text = _result.Victory ? "Victory" : "Defeated";
            titleText.color = _result.Victory ? Color.yellow : Color.red;

            //SwitchToPage(1);
            SwitchToPage(2);
        }

        private void SetupPage1()
        {
            

            // 清空旧列表
            foreach (Transform child in armyListContainer) Destroy(child.gameObject);
            // 获取最新的军队状态 (Battle.cs 应该已经更新了 ArmyStatus)
            if (Battle.Instance != null && Battle.Instance.armyStatus != null)
            {
                foreach (var status in Battle.Instance.armyStatus.battalionStatuses)
                {
                    var entryObj = Instantiate(unitEntryPrefab, armyListContainer);
                    var entry = entryObj.GetComponent<UnitResultEntry>();
                    
                    // TODO
                    entry.Initialize(status, false, false); 
                }
            }
            
            // 如果需要显示死亡单位，可以遍历 Battle.Instance.deadUnits
            // ...
        }
        
        private void SetupPage2()
        {
            foreach (Transform child in lootListContainer) Destroy(child.gameObject);

            if (_result.Loot != null)
            {
                foreach (var item in _result.Loot)
                {
                    var entryObj = Instantiate(lootEntryPrefab, lootListContainer);
                    var entry = entryObj.GetComponent<LootItem>();
                    entry.Initialize(item, itemTooltip);
                }
            }
        }
        
        private void SwitchToPage(int pageIndex)
        {
            _currentPage = pageIndex;

            if (_currentPage == 1)
            {
                page1_ArmyStatus.SetActive(true);
                page2_Loot.SetActive(false);
                SetupPage1();
                actionButtonText.text = "Continue";
            }
            else if (_currentPage == 2)
            {
                 // 延迟初始化第二页，或者在 Show 时一起初始化
                page1_ArmyStatus.SetActive(false);
                page2_Loot.SetActive(true);
                SetupPage2();
                actionButtonText.text = "Exit";
            }
        }

        private void OnActionButtonClicked()
        {
            if (_currentPage == 1)
            {
                SwitchToPage(2);
            }
            else
            {
                // 退出战斗
                if (Battle.Instance != null)
                {
                    Battle.Instance.ExitBattle();
                }
            }
        }
    }
}