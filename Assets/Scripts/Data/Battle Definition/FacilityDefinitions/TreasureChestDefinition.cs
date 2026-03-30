using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Definitions/Treasure Chest Definition")]
    public class TreasureChestDefinition : FacilityDefinition
    {
        [Header("Treasure Settings")]
        public GameObject openVfxPrefab;
        [Tooltip("开启后是否直接销毁宝箱")]
        public bool destroyOnOpen = true;
        
        [Header("Animation Timings")]
        public float preOpenDelay = 0.2f;    // 互动后，打开前的停顿期待时间
        public float animationDuration = 1f; // 宝箱打开动画的播放时长
        public float fadeOutDuration = 5f;   // 开完之后淡出消失的时间
        
        public override void OnInteract(Unit user, Facility facility)
        {
            if (user == null || facility == null || Battle.Instance == null) return;

            Debug.Log($"{user.name} 正在打开宝箱 {facility.name}...");

            // 启动协程，接管完整的开箱演出时间轴
            Battle.Instance.StartCoroutine(ChestOpenRoutine(user, facility));
        }
        
        private IEnumerator ChestOpenRoutine(Unit user, Facility facility)
        {
            UnitVisualController vis = facility.visualController;

            // ==========================================
            // 1. 停顿酝酿 (建立期待感)
            // ==========================================
            yield return new WaitForSeconds(preOpenDelay);

            // ==========================================
            // 2. 触发开箱动画与特效
            // ==========================================
            if (vis != null)
            {
                Animator animator = vis.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.SetTrigger("Open");
                }
            }

            if (openVfxPrefab != null)
            {
                Vector3 worldPos = Battle.Instance.MapToWorld(facility.position);
                Instantiate(openVfxPrefab, worldPos, Quaternion.identity);
            }

            // 等待动画播放完毕，呈现宝箱完全打开的定格画面
            yield return new WaitForSeconds(animationDuration);

            // ==========================================
            // 3. 逻辑结算：正式给予战利品
            // ==========================================
            GiveLootToUser(user, facility);

            // ==========================================
            // 4. 表现层与逻辑收尾：淡出并销毁
            // ==========================================
            if (destroyOnOpen)
            {
                if (vis != null)
                {
                    yield return Battle.Instance.StartCoroutine(FadeOutRoutine(vis, fadeOutDuration));
                }
                
                // 淡出完毕后，正式从地图和数据中移除
                Battle.Instance.RemoveUnitFromBattle(facility);
            }
        }
        
        private void GiveLootToUser(Unit user, Facility facility)
        {
            Battalion bat = user as Battalion;
            if (bat == null) return;

            // 1. 规则掉落
            if (lootRules != null)
            {
                foreach (var rule in lootRules)
                {
                    if (rule.lootTable == null) continue;
                    if (Random.Range(0, 100) < rule.dropChance)
                    {
                        var loot = rule.lootTable.Roll();
                        if (loot != null && loot.definition != null)
                        {
                            bat.AddItem(loot.definition, loot.amount);
                            Debug.Log($"从宝箱获得: {loot.definition.itemName} x{loot.amount}");
                        }
                    }
                }
            }
            
            // 2. 固定库存掉落
            if (facility.inventory != null && facility.inventory.Count > 0)
            {
                foreach (var item in facility.inventory)
                {
                    if (item != null && item.definition != null)
                    {
                        bat.AddItem(item.definition, item.amount);
                        Debug.Log($"从宝箱固有库存获得: {item.definition.itemName} x{item.amount}");
                    }
                }
                facility.inventory.Clear();
            }
        }
        
        private IEnumerator FadeOutRoutine(UnitVisualController vis, float duration)
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
                yield return null;
            }
        }

        
        
    }
}