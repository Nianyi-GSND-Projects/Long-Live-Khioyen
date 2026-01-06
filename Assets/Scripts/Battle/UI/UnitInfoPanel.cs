using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LongLiveKhioyen
{
    // 这是一个单纯的 View 组件，只负责显示数据
    public class UnitInfoPanel : MonoBehaviour
    {
        [Header("Components")]
        public CanvasGroup canvasGroup; // 方便 BattleUi 控制显隐

        [Header("Basic Info")]
        public Image portraitImage;
        public TMP_Text unitNameText;
        public TMP_Text commanderNameText;

        [Header("Unit Stats")]
        public TMP_Text soldierCountText; // 显示 "当前/最大"
        public TMP_Text moraleText;
        public TMP_Text expText;
        public TMP_Text movementText;

        [Header("Commander Attributes")]
        public TMP_Text statZhi; // 智
        public TMP_Text statXin; // 信
        public TMP_Text statRen; // 仁
        public TMP_Text statYong;// 勇
        public TMP_Text statYan; // 严

        // 刷新 UI 的主要方法
        public void UpdateUI(Unit unit)
        {
            if (unit is Battalion bat)
            {
                // 从实体中读取数据
                DisplayInfo(
                    bat.Definition.unitName,
                    bat.currentSoliders, bat.Definition.defaultMaxSolider,
                    bat.currentMurale, // 实体可能有最大士气，这里简化
                    bat.currentTraining,
                    bat.currentMovement,
                    bat.battalionCommander
                );
            }
            else if (unit is Facility fac)
            {
                unitNameText.text = "未知建筑";
                ClearStats();
            }
        }
        
        public void UpdateUI(BattalionDescriptor desc)
        {

            int baseMove = desc.Definition != null ? desc.Definition.defaultFlexibility / 10 : 0;
            
            DisplayInfo(
                desc.Definition.unitName,
                desc.currentSoliders, desc.maxSolider,
                desc.currentMurale,
                desc.currentTraining,
                baseMove, 
                desc.battalionCommander
            );
        }
        private void DisplayInfo(string uName, int curSoldier, int maxSoldier, int curMorale, int curExp, int movement, GameCommander cmd)
        {
            unitNameText.text = uName;
            soldierCountText.text = $"{curSoldier} / {maxSoldier}";
            moraleText.text = $"{curMorale}";
            expText.text = $"{curExp}";
            movementText.text = $"{movement}";
            
            
            
            if (cmd != null)
            {
                commanderNameText.text = cmd.commanderName;
                statZhi.text = cmd.Zhi.ToString();
                statXin.text = cmd.Xin.ToString();
                statRen.text = cmd.Ren.ToString();
                statYong.text = cmd.Yong.ToString();
                statYan.text = cmd.Yan.ToString();
                portraitImage.sprite = cmd.portrait;
            }
            else
            {
                commanderNameText.text = "无指挥官";
                statZhi.text = "-"; statXin.text = "-"; statRen.text = "-"; statYong.text = "-"; statYan.text = "-";
            }
        }
        

        private void ClearStats()
        {
            statZhi.text = "-";
            statXin.text = "-";
            statRen.text = "-";
            statYong.text = "-";
            statYan.text = "-";
        }
    }
}