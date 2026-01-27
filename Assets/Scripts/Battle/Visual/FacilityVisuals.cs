using UnityEngine;

namespace LongLiveKhioyen
{
    public class FacilityVisuals : UnitVisualController
    {
        private GameObject currentModelObj;

        protected override void RefreshModel()
        {
            Facility fac = _ownerUnit as Facility;
            if (fac == null || fac.Definition == null) return;

            // 假设 Facility 有 maxDurability 属性
            float maxHP = Mathf.Max(1, fac.Definition.defaultMaxDurability);
            float hpPercent = (float)fac.currentDurability / maxHP;

            GameObject targetPrefab = null;
            
            // 查找对应状态的模型
            if (fac.Definition.damageStates != null)
            {
                foreach (var state in fac.Definition.damageStates)
                {
                    if (hpPercent <= state.healthPercentage)
                    {
                        targetPrefab = state.stateModel;
                    }
                    else break; 
                }
            }

            // 如果状态没变，就不重新生成了
            // 这里为了简单，每次由 Unit 调用 RefreshVisuals 时都强制刷新
            // 实际优化可以判断 prefab 是否相同
            if (currentModelObj != null) Destroy(currentModelObj);
            
            if (targetPrefab != null)
            {
                currentModelObj = Instantiate(targetPrefab, modelContainer);
                currentModelObj.transform.localPosition = Vector3.zero;
                currentModelObj.transform.localRotation = Quaternion.identity;
            }
        }
    }
}