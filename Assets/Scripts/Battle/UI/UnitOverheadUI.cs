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
        public CanvasGroup canvasGroup;
        public TMP_Text commanderNameText;
        public TMP_Text unitInfoText;
        public Image iconImage; 
        
        [Header("Bars")]
        public Slider healthSlider;
        public Slider moraleSlider;
        public GameObject moraleBarRoot;
        
        [Header("Settings")]
        public Vector3 offset = new Vector3(0, 2.5f, 0); 

        private Transform _mainCamTransform;
        private Transform _targetUnitTransform;
        
        private Unit _targetUnit; 
        
        public void Initialize(Unit unit)
        {
            _targetUnitTransform = unit.transform;
            _targetUnit = unit;
            if (Camera.main != null) 
                _mainCamTransform = Camera.main.transform;
            
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
                        float mpPercent = (float)bat.currentMurale / Mathf.Max(1, bat.GetMaxMorale());
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
                iconImage.sprite = unit.unitDefinition.figure;
            }
            
            if (healthSlider != null)
            {
                float hpPercent = (float)unit.currentHealth / Mathf.Max(1, unit.GetMaxHealth());
                healthSlider.value = hpPercent;
            }
            
            if (unitInfoText != null)
            {
                unitInfoText.text = $"{unit.currentHealth}/{unit.GetMaxHealth()}";
            }
            
            if (iconImage != null && Battle.Instance != null)
            {
                iconImage.color = Battle.Instance.GetFactionUIColor(unit.faction);
            }
            
        }
    }
}
