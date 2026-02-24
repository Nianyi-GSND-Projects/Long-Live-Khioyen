using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LongLiveKhioyen
{
    public class ActionMenuUI : MonoBehaviour
    {
        [Header("Containers")]
        public RectTransform panelRoot;       // 整个菜单的根节点 (用来定位)
        public RectTransform mainMenuContainer; // 一级菜单容器 (Vertical Layout)
        public RectTransform subMenuContainer;  // 二级菜单容器 (Vertical Layout)

        [Header("Prefabs")]
        public GameObject actionButtonPrefab; // 按钮预制体

        [Header("Settings")]
        public Vector2 offset = new Vector2(50, -50); // 菜单相对于单位的屏幕偏移

        // 当前操作的单位
        private Unit currentUnit;

        // 初始化/隐藏
        private void Awake()
        {
            Hide();
        }

        public void Hide()
        {
            panelRoot.gameObject.SetActive(false);
            subMenuContainer.gameObject.SetActive(false); // 默认隐藏二级
        }

        public void Show(Unit unit)
        {
            currentUnit = unit;
            if (currentUnit == null) return;

            // 1. 定位逻辑 (World -> Screen)
            PositionMenu(unit.position);

            // 2. 生成一级菜单
            GenerateMainMenu();

            // 3. 默认隐藏二级菜单
            subMenuContainer.gameObject.SetActive(false);

            // 4. 显示面板
            panelRoot.gameObject.SetActive(true);
            
            // 设置 Pivot 为左上角，这样菜单向右下延伸
            panelRoot.pivot = new Vector2(0, 1);
        }

        private void PositionMenu(Vector2Int gridPos)
        {
            Vector3 worldPos = Battle.Instance.MapToWorld(gridPos);
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            
            // 应用位置 (Z轴设为0以防万一)
            screenPos.z = 0;
            panelRoot.position = screenPos;
        }

        private void GenerateMainMenu()
        {
            // 清理旧按钮
            foreach (Transform child in mainMenuContainer) Destroy(child.gameObject);

            // --- 按钮 1: 待命 (Wait) ---
            CreateButton(mainMenuContainer, "Wait","End this unit's turn", () =>
            {
                Battle.Instance.ActionWait();
                Hide();
            });

            // --- 按钮 2: 普通攻击 (Attack) ---
            // 检查是否有默认攻击手段
            bool canAttack = currentUnit.DefaultAttack != null;
            
            if (canAttack)
            {
                // 进一步检查使用条件 (例如是否被缴械)
                bool conditionMet = currentUnit.DefaultAttack.CheckUseConditions(currentUnit);
                if(!currentUnit.DefaultAttack.HasValidTargetsInRange(currentUnit)) conditionMet = false;
                CreateButton(mainMenuContainer, "Attack", currentUnit.DefaultAttack.description,() =>
                {
                    Battle.Instance.PrepareAction(currentUnit.DefaultAttack);
                }, interactable: conditionMet);
            }
            else
            {
                CreateButton(mainMenuContainer, "Attack", currentUnit.DefaultAttack.description,null, interactable: false);
            }
            
            // --- 按钮 3: 交互 (Interact) ---
            if (currentUnit is Battalion b1 && b1.DefaultInteract != null)
            {
                // 检查条件 (例如：只有在特定格子上才能交互)
                bool canInteract = b1.DefaultInteract.CheckUseConditions(currentUnit);
                if (canInteract)
                {
                    canInteract = b1.DefaultInteract.HasValidTargetsInRange(currentUnit);
                }
                CreateButton(mainMenuContainer, "Interact", currentUnit.DefaultInteract.description,() =>
                {
                    // 交互通常是立即执行，或者是选择目标
                    // 假设是立即执行 (Self Target)
                    Battle.Instance.PrepareAction(b1.DefaultInteract);
                }, interactable: canInteract);
            }
            
            // --- 按钮 4: 撤离 (Retreat) ---
            if (currentUnit is Battalion b2 && b2.DefaultRetreat != null)
            {
                // 检查条件 (IsOnExtractionPoint && HasFullMove)
                bool canRetreat = b2.DefaultRetreat.CheckUseConditions(currentUnit);
            
                CreateButton(mainMenuContainer, "Retreat", currentUnit.DefaultRetreat.description,() =>
                {
                    // 撤离是对自己的操作 (TargetCountType.Self)
                    // PrepareAction 会处理 Self 类型
                    Battle.Instance.PrepareAction(b2.DefaultRetreat);
                }, interactable: canRetreat);
            }
            // --- 按钮 5: 部队技能 (Unit Actions) ---
            bool hasUnitActions = currentUnit.runtimeUnitActions != null && currentUnit.runtimeUnitActions.Count > 0;
            CreateButton(mainMenuContainer, "Unit Skills", "Unit's unique skills",() =>
            {
                // 点击后展开二级菜单
                PopulateSubMenu(currentUnit.runtimeUnitActions);
            }, interactable: hasUnitActions);

            // --- 按钮 6: 指挥官技能 (Commander Actions) ---
            bool hasCmdActions = currentUnit.runtimeCommanderActions != null && currentUnit.runtimeCommanderActions.Count > 0;
            CreateButton(mainMenuContainer, "Commander Skills", "Commander's unique skills", () =>
            {
                // 点击后展开二级菜单
                PopulateSubMenu(currentUnit.runtimeCommanderActions);
            }, interactable: hasCmdActions);
        }
        public bool TryCloseSubMenu()
        {
            // 如果面板本身没开，或者二级菜单没开，返回 false
            if (!panelRoot.gameObject.activeSelf || !subMenuContainer.gameObject.activeSelf)
            {
                return false;
            }

            // 关闭二级菜单
            subMenuContainer.gameObject.SetActive(false);
            return true;
        }
        
        private void PopulateSubMenu(List<ActionDefinition> actions)
        {
            // 显示二级容器
            subMenuContainer.gameObject.SetActive(true);

            // 清理旧内容
            foreach (Transform child in subMenuContainer) Destroy(child.gameObject);

            // 生成技能按钮
            foreach (var action in actions)
            {
                // 检查使用条件 (决定按钮是否置灰)
                bool isUsable = action.CheckUseConditions(currentUnit);
                if (isUsable)
                {
                    isUsable = action.HasValidTargetsInRange(currentUnit);
                }
                CreateButton(subMenuContainer, action.actionName, action.description,() =>
                {
                    Battle.Instance.PrepareAction(action);
                }, interactable: isUsable);
            }
        }

        // 辅助方法：创建按钮
        private void CreateButton(Transform container, string text, string description, System.Action onClick, bool interactable = true)
        {
            GameObject btnObj = Instantiate(actionButtonPrefab, container);
            ActionButtonUI btnUI = btnObj.GetComponent<ActionButtonUI>();
            // 设置文本
            if (btnUI != null)
            {
                btnUI.Setup(text, description, onClick, interactable);
            }
            else
            {
                // Fallback: 如果 Prefab 没挂脚本，保持旧逻辑 (防止报错)
                TMP_Text tmp = btnObj.GetComponentInChildren<TMP_Text>();
                if (tmp != null) tmp.text = text;

                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = interactable;
                    btn.onClick.RemoveAllListeners();
                    if (onClick != null) btn.onClick.AddListener(() => onClick.Invoke());
                }
            }
        }
    }
}