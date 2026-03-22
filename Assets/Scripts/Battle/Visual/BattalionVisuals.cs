using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public class BattalionVisuals : UnitVisualController
    {
        private List<SoldierVisual> activeSoldiers = new List<SoldierVisual>();

        protected override void RefreshModel()
        {
            Battalion bat = _ownerUnit as Battalion;
            if (bat == null || bat.Definition == null) return;
            SoldierState currentState = bat.CurrentSoldierState;
            foreach (var s in activeSoldiers) 
            {
                if (s != null) Destroy(s.gameObject);
            }
            activeSoldiers.Clear();
            if (bat.Definition.unitModelPrefab == null) return;
            int perModel = Mathf.Max(1, bat.Definition.soldiersPerModel); 
            int modelCount = Mathf.CeilToInt((float)bat.currentSoliders / perModel);
            modelCount = Mathf.Min(modelCount, 20);
            GenerateFormation(modelCount, bat.Definition, currentState);
            CacheRenderers();
        }

        private void GenerateFormation(int count, BattalionDefinition def, SoldierState initialState)
        {
            float spacing = def.modelSpacing;
            int rowLength = Mathf.CeilToInt(Mathf.Sqrt(count)); 
            
            for (int i = 0; i < count; i++)
            {
                GameObject soldierObj = Instantiate(def.unitModelPrefab, modelContainer);
                
                float x = (i % rowLength) * spacing;
                float z = (i / rowLength) * spacing;
                
                float offsetX = (rowLength - 1) * spacing * 0.5f;
                float offsetZ = (Mathf.CeilToInt((float)count/rowLength) - 1) * spacing * 0.5f;

                Vector3 pos = new Vector3(x - offsetX, 0, z - offsetZ);

                if (pos.magnitude < 0.3f) 
                {
                    pos += Vector3.back * 0.5f;
                }

                soldierObj.transform.localPosition = pos;

                SoldierVisual sv = soldierObj.GetComponent<SoldierVisual>();
                if (sv != null)
                {
                    sv.SetupInitialVisuals(def.spriteMaterial, false);
                    
                    sv.SetState(initialState);
                    
                    activeSoldiers.Add(sv);
                }
                else
                {
                    Debug.LogError("预制体上缺少 SoldierVisual 组件！");
                }
            }
        }
        
        public void SetBattalionState(SoldierState newState)
        {
            foreach (var soldier in activeSoldiers)
            {
                if (soldier != null)
                {
                    soldier.SetState(newState);
                }
            }
        }
    }
}