using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

namespace LongLiveKhioyen
{
    public class ConstructionUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject panelRoot;
        public Transform listContainer;
        public GameObject itemPrefab; // 按钮预制体 (Button + Text + Icon)

        private void Start()
        {
            if (Battle.Instance != null)
            {
                Battle.Instance.OnActionStageChanged += HandleStageChanged;
            }
            Hide();
        }

        private void OnDestroy()
        {
            if (Battle.Instance != null)
            {
                Battle.Instance.OnActionStageChanged -= HandleStageChanged;
            }
        }

        private void HandleStageChanged(PlayerActionStage stage)
        {
            if (stage == PlayerActionStage.SelectingBuildItem)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        private void Show()
        {
            panelRoot.SetActive(true);
            RefreshList();
        }

        private void Hide()
        {
            panelRoot.SetActive(false);
        }

        private void RefreshList()
        {
            foreach (Transform child in listContainer) Destroy(child.gameObject);

            var facilities = Battle.Instance.buildableFacilities;
            if (facilities == null) return;

            foreach (var fac in facilities)
            {
                var go = Instantiate(itemPrefab, listContainer);
                
                // Setup UI
                var text = go.GetComponentInChildren<TMP_Text>();
                if (text != null) text.text = fac.unitName;
                
                var img = go.GetComponentInChildren<Image>(); // 假设有 Icon
                // if (img != null) img.sprite = fac.icon;

                var btn = go.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => OnSelectFacility(fac));
                }
            }
        }

        private void OnSelectFacility(FacilityDefinition fac)
        {
            Battle.Instance.PendingFacility = fac;
    
            // 2. 获取单位的建造动作
            if (Battle.Instance.SelectedUnit is Battalion bat && bat.Definition.defaultConstructAction != null)
            {
                Battle.Instance.PrepareAction(bat.Definition.defaultConstructAction);
            }
    
            Hide();
        }
    }
}