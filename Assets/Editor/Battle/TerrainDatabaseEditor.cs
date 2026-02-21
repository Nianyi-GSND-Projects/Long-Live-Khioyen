using UnityEngine;
using UnityEditor;
using LongLiveKhioyen;
using System.Collections.Generic;

[CustomEditor(typeof(TerrainDatabase))]
public class TerrainDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Collect All Terrains")) Collect((TerrainDatabase)target);
    }

    private void Collect(TerrainDatabase db)
    {
        string[] guids = AssetDatabase.FindAssets("t:TerrainDefinition");
        List<TerrainDefinition> all = new List<TerrainDefinition>();
        foreach (string guid in guids)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TerrainDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) all.Add(asset);
        }

        // 假设你已经添加了 id 字段
        AssignIds(all, (t) => t.id, (t, id) => t.id = id);

        db.terrainDefinitions = all;
        db.terrainDefinitions.Sort((a, b) => a.id.CompareTo(b.id));
        
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"Collected {all.Count} terrains.");
    }

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