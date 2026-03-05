using UnityEngine;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    public class FacilityVisuals : UnitVisualController
    {
        private GameObject currentModelObj; // 用于单模型
        private List<GameObject> activeFormationModels = new List<GameObject>(); // 用于多模型
        private GameObject lastUsedPrefab;

        protected override void RefreshModel()
        {
            Facility fac = _ownerUnit as Facility;
            if (fac == null || fac.Definition == null) return;
            FacilityDefinition def = fac.Definition;

            GameObject targetPrefab = GetTargetPrefab(fac, def);

            if (lastUsedPrefab == targetPrefab) return;

            ClearCurrentModels();

            if (targetPrefab != null)
            {
                if (def.useFormationDisplay)
                {
                    GenerateFormation(targetPrefab, def); // 传递整个 definition
                }
                else
                {
                    currentModelObj = Instantiate(targetPrefab, modelContainer);
                    currentModelObj.transform.localPosition = Vector3.zero;
                    currentModelObj.transform.localRotation = Quaternion.identity;
                    
                    // [新增] 为单个模型应用 Billboard
                    if (def.useBillboard)
                    {
                        ApplyBillboard(currentModelObj);
                    }
                }
            }
            
            lastUsedPrefab = targetPrefab;
            CacheRenderers();
        }
        
        private GameObject GetTargetPrefab(Facility fac, FacilityDefinition def)
        {
            // 如果未建成且有建设中模型
            if (!fac.isConstructed && def.constructionStagePrefabs.Count > 0)
            {
                float maxHP = Mathf.Max(1, def.defaultMaxDurability);
                float progress = (float)fac.currentDurability / maxHP;
                
                // 计算应该选择哪个阶段的模型
                int stageIndex = Mathf.FloorToInt(progress * def.constructionStagePrefabs.Count);
                stageIndex = Mathf.Clamp(stageIndex, 0, def.constructionStagePrefabs.Count - 1);
                
                return def.constructionStagePrefabs[stageIndex];
            }
            
            // 否则，返回默认的完整模型
            return def.unitModelPrefab;
        }

        private void GenerateFormation(GameObject prefab, FacilityDefinition def)
        {
            int count = def.formationInstanceCount;
            float spacing = def.formationSpacing;
            int rowLength = Mathf.CeilToInt(Mathf.Sqrt(count)); 
            
            for (int i = 0; i < count; i++)
            {
                GameObject model = Instantiate(prefab, modelContainer);
                
                float x = (i % rowLength) * spacing;
                float z = (i / rowLength) * spacing;
                
                float offsetX = (rowLength - 1) * spacing * 0.5f;
                float offsetZ = (Mathf.CeilToInt((float)count / rowLength) - 1) * spacing * 0.5f;

                model.transform.localPosition = new Vector3(x - offsetX, 0, z - offsetZ);
                
                // [新增] 为阵列中的每个模型应用 Billboard
                if (def.useBillboard)
                {
                    ApplyBillboard(model);
                }
                
                activeFormationModels.Add(model);
            }
        }

        private void ClearCurrentModels()
        {
            if (currentModelObj != null)
            {
                Destroy(currentModelObj);
                currentModelObj = null;
            }
            foreach (var model in activeFormationModels)
            {
                Destroy(model);
            }
            activeFormationModels.Clear();
        }
        
        private void ApplyBillboard(GameObject target)
        {
            if (target == null) return;

            if (target.GetComponent<Billboard>() == null)
            {
                target.AddComponent<Billboard>();
            }
        }
    }
}