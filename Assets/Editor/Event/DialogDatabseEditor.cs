using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using LongLiveKhioyen;

[CustomEditor(typeof(DialogDatabase))]
public class DialogDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        DialogDatabase db = (DialogDatabase)target;

        if (GUILayout.Button("Collect All Dialogs"))
        {
            CollectDialogs(db);
        }
    }

    private void CollectDialogs(DialogDatabase db)
    {
        // 1. 查找所有资源
        string[] guids = AssetDatabase.FindAssets("t:DialogChainAction");
        List<DialogChainAction> allDialogs = new List<DialogChainAction>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            DialogChainAction dialog = AssetDatabase.LoadAssetAtPath<DialogChainAction>(path);
            if (dialog != null) allDialogs.Add(dialog);
        }

        // 2. 整理 ID
        HashSet<int> usedIds = new HashSet<int>();
        List<DialogChainAction> toAssign = new List<DialogChainAction>();

        // 第一轮：保留合法的现有 ID
        foreach (var dialog in allDialogs)
        {
            if (dialog.id > 0 && !usedIds.Contains(dialog.id))
            {
                usedIds.Add(dialog.id);
            }
            else
            {
                toAssign.Add(dialog); // ID 为 0 或重复，待分配
            }
        }

        // 3. 分配新 ID
        int nextId = 1;
        foreach (var dialog in toAssign)
        {
            while (usedIds.Contains(nextId)) nextId++;
            
            dialog.id = nextId;
            usedIds.Add(nextId);
            EditorUtility.SetDirty(dialog); // 标记修改，确保保存
        }

        // 4. 更新 Database 列表
        db.dialogs = allDialogs;
        db.dialogs.Sort((a, b) => a.id.CompareTo(b.id));

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets(); // 保存所有修改 (包括 SO 和 Database)
        Debug.Log($"Collected {db.dialogs.Count} dialogs. Assigned {toAssign.Count} new IDs.");
    }
}