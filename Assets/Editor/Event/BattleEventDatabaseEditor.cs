using UnityEngine;
using UnityEditor;
using LongLiveKhioyen;
using System.Collections.Generic;

[CustomEditor(typeof(BattleEventDatabase))]
public class BattleEventDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        BattleEventDatabase db = (BattleEventDatabase)target;

        if (GUILayout.Button("Collect All Battle Events"))
        {
            CollectEvents(db);
        }
    }

    private void CollectEvents(BattleEventDatabase db)
    {
        // 1. 查找所有资源
        string[] guids = AssetDatabase.FindAssets("t:BattleEventDefinition");
        List<BattleEventDefinition> allEvents = new List<BattleEventDefinition>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BattleEventDefinition evt = AssetDatabase.LoadAssetAtPath<BattleEventDefinition>(path);
            if (evt != null) allEvents.Add(evt);
        }

        // 2. 整理 ID
        HashSet<int> usedIds = new HashSet<int>();
        List<BattleEventDefinition> toAssign = new List<BattleEventDefinition>();

        // 第一轮：保留合法的现有 ID
        foreach (var evt in allEvents)
        {
            if (evt.id > 0 && !usedIds.Contains(evt.id))
            {
                usedIds.Add(evt.id);
            }
            else
            {
                toAssign.Add(evt); // ID 为 0 或重复，待分配
            }
        }

        // 3. 分配新 ID
        int nextId = 1;
        foreach (var evt in toAssign)
        {
            while (usedIds.Contains(nextId)) nextId++;
            
            evt.id = nextId;
            usedIds.Add(nextId);
            EditorUtility.SetDirty(evt); // 标记修改
        }

        // 4. 更新 Database 列表
        db.events = allEvents;
        db.events.Sort((a, b) => a.id.CompareTo(b.id));

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"Collected {db.events.Count} battle events. Assigned {toAssign.Count} new IDs.");
    }
}