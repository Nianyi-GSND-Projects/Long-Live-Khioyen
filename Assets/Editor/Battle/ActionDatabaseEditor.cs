using UnityEngine;
using UnityEditor;
using LongLiveKhioyen;
using System.Collections.Generic;
using System.Reflection; // 用于访问私有字段

[CustomEditor(typeof(ActionDatabase))]
public class ActionDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        ActionDatabase db = (ActionDatabase)target;
        if (GUILayout.Button("Collect All Actions")) CollectActions(db);
    }

    private void CollectActions(ActionDatabase db)
    {
        // 使用反射访问私有字段 actionDefinitions
        FieldInfo field = typeof(ActionDatabase).GetField("actionDefinitions", BindingFlags.NonPublic | BindingFlags.Instance);
        List<ActionDefinition> list = (List<ActionDefinition>)field.GetValue(db);
        if (list == null) list = new List<ActionDefinition>();

        string[] guids = AssetDatabase.FindAssets("t:ActionDefinition");
        List<ActionDefinition> all = new List<ActionDefinition>();
        foreach (string guid in guids)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ActionDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) all.Add(asset);
        }

        AssignIds(all, (a) => a.actionId, (a, id) => a.actionId = id);
        
        list.Clear();
        list.AddRange(all);
        list.Sort((a, b) => a.actionId.CompareTo(b.actionId));
        
        field.SetValue(db, list);
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"Collected {list.Count} actions.");
    }

    // 通用 ID 分配逻辑
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