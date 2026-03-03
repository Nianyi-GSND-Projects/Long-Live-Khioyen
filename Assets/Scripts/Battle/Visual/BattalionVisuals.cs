using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public class BattalionVisuals : UnitVisualController
    {
        private List<GameObject> activeSoldiers = new List<GameObject>();

        protected override void RefreshModel()
        {
            Battalion bat = _ownerUnit as Battalion;
            if (bat == null || bat.Definition == null) return;

            foreach (var s in activeSoldiers) Destroy(s);
            activeSoldiers.Clear();

            if (bat.Definition.unitModelPrefab == null) return;

            int perModel = Mathf.Max(1, bat.Definition.soldiersPerModel); 
            int modelCount = Mathf.CeilToInt((float)bat.currentSoliders / perModel);
            modelCount = Mathf.Min(modelCount, 20); // 上限限制

            GenerateFormation(modelCount, bat.Definition);
        }

        private void GenerateFormation(int count, BattalionDefinition def)
        {
            float spacing = def.modelSpacing;
            int rowLength = Mathf.CeilToInt(Mathf.Sqrt(count)); 
            
            for (int i = 0; i < count; i++)
            {
                GameObject soldier = Instantiate(def.unitModelPrefab, modelContainer);
                
                // 计算位置
                float x = (i % rowLength) * spacing;
                float z = (i / rowLength) * spacing;
                
                // 居中偏移
                float offsetX = (rowLength - 1) * spacing * 0.5f;
                float offsetZ = (Mathf.CeilToInt((float)count/rowLength) - 1) * spacing * 0.5f;

                Vector3 pos = new Vector3(x - offsetX, 0, z - offsetZ);

                // [避让逻辑] 旗帜在 (0,0,0)，如果士兵也在中心，稍微移开一点
                if (pos.magnitude < 0.3f) 
                {
                    // 简单的避让：往后挪一点
                    pos += Vector3.back * 0.5f;
                }

                soldier.transform.localPosition = pos;

                
                var sr = soldier.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    sr.flipX = Random.value > 0.5f;
                    
                    // sr.color = Battle.Instance.GetFactionColor(_ownerUnit.faction); // 需在 Battle 中实现 GetFactionColor 返回 Color
                }
                
                activeSoldiers.Add(soldier);
            }
        }
    }
}