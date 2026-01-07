using UnityEngine;
using UnityEditor; // 必须引用这个
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    // 绑定到 TerrainDatabase 类型
    [CustomEditor(typeof(TerrainDB))]
    public class TerrainDBEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // 1. 绘制默认的 Inspector (显示列表本身)
            DrawDefaultInspector();

            // 2. 获取当前选中的 ScriptableObject 对象
            TerrainDB database = (TerrainDB)target;

            GUILayout.Space(15);

            // 3. 绘制绿色大按钮
            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f); 
            if (GUILayout.Button("搜集所有地形定义 (Collect All Terrains)", GUILayout.Height(40)))
            {
                CollectTerrains(database);
            }
            GUI.backgroundColor = Color.white; // 恢复颜色

            GUILayout.Space(10);
            EditorGUILayout.HelpBox("点击按钮将自动查找项目中所有的 TerrainDefinition 并填充到上方列表。", MessageType.Info);
        }

        private void CollectTerrains(TerrainDB database)
        {
            // 初始化列表防止空引用
            if (database.terrainDefinitions == null)
                database.terrainDefinitions = new List<TerrainDefinition>();

            // 记录旧数量用于显示日志
            int oldCount = database.terrainDefinitions.Count;
            
            // 清空列表，准备重新填充
            database.terrainDefinitions.Clear();

            // === 核心魔法：搜索所有类型为 TerrainDefinition 的资源 ===
            string[] guids = AssetDatabase.FindAssets("t:TerrainDefinition");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TerrainDefinition terrainDef = AssetDatabase.LoadAssetAtPath<TerrainDefinition>(path);

                if (terrainDef != null)
                {
                    // 只有当列表中还没有这个地形时才添加 (虽然Clear了通常不需要判断，但为了保险)
                    if (!database.terrainDefinitions.Contains(terrainDef))
                    {
                        database.terrainDefinitions.Add(terrainDef);
                    }
                }
            }

            // === 关键：标记脏数据，强制 Unity 保存更改 ===
            EditorUtility.SetDirty(database);

            Debug.Log($"<color=green><b>【地形库更新】</b> 操作完成！</color>\n" +
                      $"旧数量: {oldCount}  ->  新数量: {database.terrainDefinitions.Count}");
        }
    }
}