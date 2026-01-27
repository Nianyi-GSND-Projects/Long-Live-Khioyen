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
        
        public void Initialize(Unit unit)
        {
            // 记录单位的 Transform，用于跟随位置
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

            // 1. 位置跟随：始终位于单位头顶固定高度
            // 这样做的好处是：即使单位模型旋转了（比如转身），UI 也不会跟着公转
            transform.position = _targetUnitTransform.position + offset;

            // 2. 旋转跟随（Billboard）：始终保持与摄像机相同的旋转角度
            if (_mainCamTransform != null)
            {
                transform.rotation = _mainCamTransform.rotation;
            }
        }
        
        public void UpdateInfo(Unit unit)
        {
            if (unit is Battalion bat)
            {
                string cmdName = bat.battalionCommander != null ? bat.battalionCommander.commanderName : "无指挥官";
                commanderNameText.text = cmdName;
                unitInfoText.text = $"{bat.Definition.unitName} | {bat.currentSoliders}";
            }
            else if (unit is Facility fac)
            {
                commanderNameText.text = ""; // 设施通常没有指挥官
                unitInfoText.text = $"{fac.Definition.unitName} | HP:{fac.currentDurability}";
            }
            
            
            if (unit.unitDefinition != null && iconImage != null)
            {
                iconImage.sprite = unit.unitDefinition.figure;
            }
        }
    }
}
