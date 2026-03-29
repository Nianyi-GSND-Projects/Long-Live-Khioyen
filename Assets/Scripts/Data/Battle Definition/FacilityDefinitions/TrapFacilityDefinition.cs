using UnityEngine;
using System.Collections;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Definitions/Trap Facility")]
    public class TrapFacilityDefinition : FacilityDefinition
    {
        [Header("Trap Settings")]
        [Tooltip("对进入的单位造成的伤害")]
        public int damage = 100;

        [Tooltip("触发后是否销毁自身")]
        public bool consumeOnTrigger = true;

        [Tooltip("是否对敌方单位触发")]
        public bool triggerOnEnemies = true;

        [Tooltip("是否对友方单位触发")]
        public bool triggerOnAllies = false;
        
        [Tooltip("是否阻止移动")]
        public bool preventMovement = true;
        
        [Header("Trap Visuals")]
        public Sprite armedSprite;
        public Sprite triggeredSprite;
        public float fadeOutDuration = 0.5f;
        
        public GameObject triggerVfx;

        public IEnumerator TriggerCoroutine(Unit unit, Facility facility, bool showVisuals)
        {
            float t = BattleParam.Instance.actionAnimationDuration;
            BattleCameraController camController = Battle.Instance.inputController?.cameraController;
            Battalion bat = unit as Battalion;
            UnitVisualController trapVis = facility.visualController;
            
            if (showVisuals)
            {
                facility.IsVisible = true;
                if (trapVis != null)
                {
                    
                    // 强制变为可见（不透明）
                    trapVis.SetOverallVisibility(true, 1f);

                    // 通知视觉组件按新数据刷新（此时会自动变为不透明）
                    trapVis.RefreshVisuals(); 
            
                    // 隐藏 OverheadUI 保持陷阱神秘感
                    if (trapVis.overheadUI != null) 
                        trapVis.overheadUI.gameObject.SetActive(false);

                    SetTrapSprite(trapVis, armedSprite);
                }

                // 摄像机聚焦到踩中陷阱的单位
                if (camController != null)
                {
                    camController.FocusOnPosition(Battle.Instance.MapToWorld(unit.position), 
                        BattleParam.Instance.focusCameraDistance, 
                        BattleParam.Instance.cameraTransitionDuration);
                }

                // 【核心停顿】给玩家一小段时间意识到踩到陷阱了
                yield return new WaitForSeconds(t);

                // ==========================================
                // 2. 表现层：陷阱夹起，单位受击
                // ==========================================
                SetTrapSprite(trapVis, triggeredSprite);
                if (bat != null) bat.CurrentSoldierState = SoldierState.Hit;

                // 如果有特效，在这里 Instantiate(triggerVfx, ...)

                // 再次停顿，展示受击瞬间
                yield return new WaitForSeconds(t);
            }
            
            unit.TakeDamage(damage);
            
            // 假设陷阱是一次性的，将其生命值归零标记为死亡
            facility.TakeDamage(facility.currentHealth);

            // ==========================================
            // 4. 表现层：陷阱淡出消失
            // ==========================================
            if (showVisuals && trapVis != null)
            {
                yield return Battle.Instance.StartCoroutine(FadeOutTrapRoutine(trapVis, fadeOutDuration));
            }
            
            yield break;
        }
        
        private void SetTrapSprite(UnitVisualController vis, Sprite sprite)
        {
            SpriteRenderer sr = vis.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sprite != null)
            {
                sr.sprite = sprite;
            }
        }
        
        private IEnumerator FadeOutTrapRoutine(UnitVisualController vis, float duration)
        {
            SpriteRenderer sr = vis.GetComponentInChildren<SpriteRenderer>();
            if (sr == null) yield break;

            Color c = sr.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(1f, 0f, elapsed / duration);
                sr.color = c;
                yield return null; // 等待下一帧
            }

            // 淡出结束后，彻底隐藏
            sr.gameObject.SetActive(false);
        }
        public void Trigger(Unit target, Facility trapInstance)
        {
            if (target == null || trapInstance == null) return;

            bool shouldTrigger = (triggerOnEnemies && target.faction != trapInstance.faction) ||
                                 (triggerOnAllies && target.faction == trapInstance.faction);

            if (shouldTrigger)
            {
                Debug.Log($"{target.name} 踩中了陷阱 {trapInstance.name}!");
                
                target.TakeDamage(damage, trapInstance);
                
                if (consumeOnTrigger)
                {
                    trapInstance.currentDurability = -1;
                    Battle.Instance.MarkUnitDirty(trapInstance);
                    Battle.Instance.CheckDeath(trapInstance);
                }
            }
        }
    }
}