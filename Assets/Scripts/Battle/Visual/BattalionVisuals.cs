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

            // 清理旧兵模 (注意：不要清理 _activeFlag)
            foreach (var s in activeSoldiers) Destroy(s);
            activeSoldiers.Clear();

            // 如果没有配置兵模，就只显示基类的旗帜，直接返回
            if (bat.Definition.soldierModelPrefab == null) return;

            // 计算士兵数量
            int perModel = Mathf.Max(1, bat.Definition.soldiersPerModel); 
            int modelCount = Mathf.CeilToInt((float)bat.currentSoliders / perModel);
            modelCount = Mathf.Min(modelCount, 20); // 上限限制

            // 生成方阵
            GenerateFormation(modelCount, bat.Definition);
        }

        private void GenerateFormation(int count, BattalionDefinition def)
        {
            float spacing = def.modelSpacing;
            int rowLength = Mathf.CeilToInt(Mathf.Sqrt(count)); 
            
            for (int i = 0; i < count; i++)
            {
                GameObject soldier = Instantiate(def.soldierModelPrefab, modelContainer);
                
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
                
                // 随机朝向 (但大体朝前)
                soldier.transform.localRotation = Quaternion.Euler(0, Random.Range(-15, 15), 0);
                
                activeSoldiers.Add(soldier);
            }
        }
    }
}