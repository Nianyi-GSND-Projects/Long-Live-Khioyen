using UnityEngine;
using UnityEditor; // 必须引用
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    [CustomEditor(typeof(UnitDatabase))]
    public class UnitDatabaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 1. 绘制默认的 Inspector (显示 List 和 Enum)
            DrawDefaultInspector();

            // 获取目标对象
            UnitDatabase database = (UnitDatabase)target;

            GUILayout.Space(20); // 添加一点间距

            // 2. 添加按钮
            if (GUILayout.Button("Find & Populate Units", GUILayout.Height(40)))
            {
                PopulateDatabase(database);
            }
            
            // 提示信息
            EditorGUILayout.HelpBox($"Clicking the button will search the project for all {database.databaseType} assets and replace the list below.", MessageType.Info);
        }

        private void PopulateDatabase(UnitDatabase db)
        {
            // 记录撤销操作，防止误点按钮导致数据丢失无法恢复
            Undo.RecordObject(db, "Populate Unit Database");

            // 准备搜索过滤器字符串 (t:Type)
            string searchFilter = "";

            switch (db.databaseType)
            {
                case UnitDatabaseType.BattalionOnly:
                    // 搜索所有继承自 BattalionDefinition 的资源
                    searchFilter = "t:BattalionDefinition"; 
                    break;
                case UnitDatabaseType.FacilityOnly:
                    // 搜索所有继承自 FacilityDefinition 的资源
                    searchFilter = "t:FacilityDefinition";
                    break;
                case UnitDatabaseType.All:
                    // 搜索基类 (会包含所有子类)
                    searchFilter = "t:UnitDefinition";
                    break;
            }

            // 使用 AssetDatabase 查找 GUIDs
            string[] guids = AssetDatabase.FindAssets(searchFilter);
            
            // 初始化新列表
            List<UnitDefinition> newUnits = new List<UnitDefinition>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UnitDefinition unit = AssetDatabase.LoadAssetAtPath<UnitDefinition>(path);

                if (unit != null)
                {
                    newUnits.Add(unit);
                }
            }

            // 更新数据
            db.unitDefinitions = newUnits;

            // 标记对象为已修改 (Dirty)，确保 Unity 保存更改
            EditorUtility.SetDirty(db);
            
            // 打印日志
            Debug.Log($"<color=green>UnitDatabase Updated:</color> Found {newUnits.Count} items for mode {db.databaseType}.");
        }
    }
}