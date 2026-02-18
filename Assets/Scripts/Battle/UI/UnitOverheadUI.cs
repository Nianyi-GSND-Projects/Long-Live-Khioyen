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
        
        [Header("Settings")]
        public Vector3 offset = new Vector3(0, 2.5f, 0); 

        private Transform _mainCamTransform;
        private Transform _targetUnitTransform;
        
        private Unit _targetUnit; 
        
        public void Initialize(Unit unit)
        {
            _targetUnitTransform = unit.transform;
            
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
        }
        
        public void UpdateInfo(Unit unit)
        {
            if (unit is Battalion bat)
            {
                string cmdName = bat.battalionCommander != null ? bat.battalionCommander.commanderName : "";
                commanderNameText.text = cmdName;
                unitInfoText.text = $"{bat.Definition.unitName} | {bat.currentSoliders}";
            }
            else if (unit is Facility fac)
            {
                commanderNameText.text = "";
                unitInfoText.text = $"{fac.Definition.unitName} | HP:{fac.currentDurability}";
            }
            
            
            if (unit.unitDefinition != null && iconImage != null)
            {
                iconImage.sprite = unit.unitDefinition.figure;
            }
        }
    }
}
