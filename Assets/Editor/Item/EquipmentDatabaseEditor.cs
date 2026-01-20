using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    [CustomEditor(typeof(EquipmentDatabase))]
    public class EquipmentDatabaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EquipmentDatabase db = (EquipmentDatabase)target;

            GUILayout.Space(15);
            GUI.backgroundColor = new Color(1f, 0.7f, 0.7f); // 浅红色

            if (GUILayout.Button("搜集所有装备 (Collect All Equipment)", GUILayout.Height(40)))
            {
                CollectEquipment(db);
            }
            GUI.backgroundColor = Color.white;
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("自动查找项目中所有 EquipmentDefinition 并注册。", MessageType.Info);
        }

        private void CollectEquipment(EquipmentDatabase db)
        {
            if (db.equipments == null) db.equipments = new List<EquipmentDefinition>();
            
            int oldCount = db.equipments.Count;
            db.equipments.Clear();

            string[] guids = AssetDatabase.FindAssets("t:EquipmentDefinition");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EquipmentDefinition eq = AssetDatabase.LoadAssetAtPath<EquipmentDefinition>(path);
                if (eq != null)
                {
                    db.equipments.Add(eq);
                }
            }

            db.equipments.Sort((a, b) => a.equipmentId.CompareTo(b.equipmentId));

            EditorUtility.SetDirty(db);
            Debug.Log($"<color=orange><b>【装备库更新】</b></color> 旧数量: {oldCount} -> 新数量: {db.equipments.Count}");
        }
    }
}