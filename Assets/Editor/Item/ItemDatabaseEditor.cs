using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    [CustomEditor(typeof(ItemDatabase))]
    public class ItemDatabaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ItemDatabase db = (ItemDatabase)target;

            GUILayout.Space(15);
            GUI.backgroundColor = new Color(0.7f, 1f, 1f); // 浅蓝色

            if (GUILayout.Button("搜集所有物品 (Collect All Items)", GUILayout.Height(40)))
            {
                CollectItems(db);
            }
            GUI.backgroundColor = Color.white;
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("自动查找项目中所有 ItemDefinition 并注册。", MessageType.Info);
        }

        private void CollectItems(ItemDatabase db)
        {
            if (db.items == null) db.items = new List<ItemDefinition>();
            
            int oldCount = db.items.Count;
            db.items.Clear();

            // 查找所有 ItemDefinition
            string[] guids = AssetDatabase.FindAssets("t:ItemDefinition");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (item != null)
                {
                    db.items.Add(item);
                }
            }

            EditorUtility.SetDirty(db);
            Debug.Log($"<color=cyan><b>【物品库更新】</b></color> 旧数量: {oldCount} -> 新数量: {db.items.Count}");
        }
    }
}