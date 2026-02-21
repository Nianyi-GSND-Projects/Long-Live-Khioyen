using UnityEngine;
using UnityEditor;
using LongLiveKhioyen;
using System.Collections.Generic;

[CustomEditor(typeof(UnitDatabase))]
public class UnitDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        UnitDatabase db = (UnitDatabase)target;

        if (GUILayout.Button("Collect Units (Global ID Check)"))
        {
            CollectUnits(db);
        }
    }

    private void CollectUnits(UnitDatabase db)
    {
        // 1. 扫描项目中所有的 UnitDefinition (包括 Battalion 和 Facility)
        string[] guids = AssetDatabase.FindAssets("t:UnitDefinition");
        List<UnitDefinition> allGlobalUnits = new List<UnitDefinition>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UnitDefinition unit = AssetDatabase.LoadAssetAtPath<UnitDefinition>(path);
            if (unit != null) allGlobalUnits.Add(unit);
        }

        // 2. 全局 ID 分配 (确保所有单位 ID 唯一且持久)
        AssignGlobalIds(allGlobalUnits);

        // 3. 根据当前 Database 的类型进行筛选
        List<UnitDefinition> filteredUnits = new List<UnitDefinition>();

        foreach (var unit in allGlobalUnits)
        {
            bool include = false;
            switch (db.databaseType)
            {
                case UnitDatabaseType.All:
                    include = true;
                    break;
                case UnitDatabaseType.BattalionOnly:
                    include = (unit is BattalionDefinition); // 假设你有这个子类
                    // 或者 include = (unit.unitType == UnitType.Battalion);
                    break;
                case UnitDatabaseType.FacilityOnly:
                    include = (unit is FacilityDefinition); // 假设你有这个子类
                    // 或者 include = (unit.unitType == UnitType.Facility);
                    break;
            }

            if (include)
            {
                filteredUnits.Add(unit);
            }
        }

        // 4. 更新 Database
        db.unitDefinitions = filteredUnits;
        db.unitDefinitions.Sort((a, b) => a.id.CompareTo(b.id));

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"Collected {filteredUnits.Count} units into database (Type: {db.databaseType}). Scanned {allGlobalUnits.Count} total units.");
    }

    private void AssignGlobalIds(List<UnitDefinition> allUnits)
    {
        HashSet<int> usedIds = new HashSet<int>();
        List<UnitDefinition> toAssign = new List<UnitDefinition>();

        // 第一轮：保留合法的现有 ID
        foreach (var unit in allUnits)
        {
            if (unit.id > 0 && !usedIds.Contains(unit.id))
            {
                usedIds.Add(unit.id);
            }
            else
            {
                toAssign.Add(unit); // ID 为 0 或重复
            }
        }

        // 第二轮：分配新 ID
        int nextId = 1;
        foreach (var unit in toAssign)
        {
            while (usedIds.Contains(nextId)) nextId++;
            
            unit.id = nextId;
            usedIds.Add(nextId);
            EditorUtility.SetDirty(unit); // 标记 SO 修改
        }
    }
}