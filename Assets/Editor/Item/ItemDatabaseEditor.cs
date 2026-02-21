using UnityEngine;
using UnityEditor;
using LongLiveKhioyen;
using System.Collections.Generic;

[CustomEditor(typeof(ItemDatabase))]
public class ItemDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Collect All Items")) Collect((ItemDatabase)target);
    }

    private void Collect(ItemDatabase db)
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemDefinition");
        List<ItemDefinition> all = new List<ItemDefinition>();
        foreach (string guid in guids)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) all.Add(asset);
        }

        // 假设你已经添加了 id 字段 (注意：不是 itemId 字符串)
        AssignIds(all, (i) => i.id, (i, id) => i.id = id);

        db.items = all;
        db.items.Sort((a, b) => a.id.CompareTo(b.id));
        
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"Collected {all.Count} items.");
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