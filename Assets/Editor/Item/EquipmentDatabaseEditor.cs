using UnityEngine;
using UnityEditor;
using LongLiveKhioyen;
using System.Collections.Generic;

[CustomEditor(typeof(EquipmentDatabase))]
public class EquipmentDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Collect All Equipments")) Collect((EquipmentDatabase)target);
    }

    private void Collect(EquipmentDatabase db)
    {
        string[] guids = AssetDatabase.FindAssets("t:EquipmentDefinition");
        List<EquipmentDefinition> all = new List<EquipmentDefinition>();
        foreach (string guid in guids)
        {
            var asset = AssetDatabase.LoadAssetAtPath<EquipmentDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) all.Add(asset);
        }

        AssignIds(all, (e) => e.equipmentId, (e, id) => e.equipmentId = id);

        db.equipments = all;
        db.equipments.Sort((a, b) => a.equipmentId.CompareTo(b.equipmentId));
        
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"Collected {all.Count} equipments.");
    }
    
    // ... Copy AssignIds helper ...
    private void AssignIds<T>(List<T> items, System.Func<T, int> getId, System.Action<T, int> setId) where T : Object
    {
        HashSet<int> used = new HashSet<int>();
        List<T> toAssign = new List<T>();
        foreach (var item in items)
        {
            int id = getId(item);
            if (id > 0 && !used.Contains(id)) used.Add(id);
            else toAssign.Add(item);
        }
        int next = 1;
        foreach (var item in toAssign)
        {
            while (used.Contains(next)) next++;
            setId(item, next);
            used.Add(next);
            EditorUtility.SetDirty(item);
        }
    }
}