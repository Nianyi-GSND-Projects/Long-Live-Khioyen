using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LongLiveKhioyen
{
    public class UnitOverheadUI : MonoBehaviour
    {
        [Header("UI References")]
        public CanvasGroup _canvasGroup;
        public TMP_Text commanderNameText;
        public TMP_Text unitInfoText;
        public Image iconImage;
        public Image factionColorImage;
        
        [Header("Bars")]
        public Slider healthSlider;
        public Slider moraleSlider;
        public GameObject moraleBarRoot;
        
        [Header("Settings")]
        public Vector3 offset = new Vector3(0, 2.5f, 0); 

        private Transform _mainCamTransform;
        private Transform _targetUnitTransform;
        
        private Unit _targetUnit; 
        
        void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        public void Initialize(Unit unit)
        {
            _targetUnitTransform = unit.transform;
            _targetUnit = unit;
            if (Camera.main != null) 
                _mainCamTransform = Camera.main.transform;
            
            if (factionColorImage != null && Battle.Instance != null)
            {
                factionColorImage.color = Battle.Instance.GetFactionUIColor(unit.faction);
            }

            UpdateInfo(unit);
        }

        void LateUpdate()
        {
            if (_targetUnitTransform == null)
            {
                Destroy(gameObject);
                return;
            }
            
            if (!_targetUnitTransform.gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false); 
                return;
            }
            
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            transform.position = _targetUnitTransform.position + offset;

            if (_mainCamTransform != null)
            {
                transform.rotation = _mainCamTransform.rotation;
            }
            
            if (_targetUnit != null)
            {
                UpdateInfo(_targetUnit);
            }
        }
        
        public void UpdateInfo(Unit unit)
        {
            if (unit is Battalion bat)
            {
                commanderNameText.text = bat.battalionCommander != null ? bat.battalionCommander.commanderName : "";
                if (moraleBarRoot != null)
                {
                    moraleBarRoot.SetActive(true);
                    if (moraleSlider != null)
                    {
                        float mpPercent = (float)bat.currentMorale / Mathf.Max(1, bat.GetStat(StatType.MaxMorale));
                        moraleSlider.value = mpPercent;
                    }
                }
                    
                else
                    moraleBarRoot.SetActive(false);
            }
            else if (unit is Facility fac)
            {
                commanderNameText.text = fac.unitDefinition.unitName;
                moraleBarRoot.SetActive(false);
            }
            
            
            if (unit.unitDefinition != null && iconImage != null)
            {
                iconImage.sprite = unit.unitDefinition.icon;
            }
            
            if (healthSlider != null)
            {
                float hpPercent = (float)unit.currentHealth / Mathf.Max(1, unit.GetStat(StatType.MaxHealth));
                healthSlider.value = hpPercent;
            }
            
            if (unitInfoText != null)
            {
                unitInfoText.text = $"{unit.currentHealth}/{unit.GetStat(StatType.MaxHealth)}";
            }
            
            if (iconImage != null && Battle.Instance != null)
            {
                iconImage.color = Battle.Instance.GetFactionUIColor(unit.faction);
            }
            
        }
        
        public void SetAlpha(float alpha)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = alpha;
                // 如果完全透明（或透明度很低），则使其不可交互/不阻挡射线
                _canvasGroup.interactable = alpha > 0.01f; 
                _canvasGroup.blocksRaycasts = alpha > 0.01f; 
            }
        }
    }
}
