using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    // 注意：这里的 typeof 必须是你存放 List<CommanderTemplateSO> 的那个类
    // 也就是上一轮对话中的 CommanderSystemSettings
    [CustomEditor(typeof(CommanderSystemSettings))]
    public class CommanderSettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            CommanderSystemSettings settings = (CommanderSystemSettings)target;

            GUILayout.Space(15);

            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
            if (GUILayout.Button("搜罗天下名将 (Auto Collect Commanders)", GUILayout.Height(40)))
            {
                CollectCommanders(settings);
            }
            GUI.backgroundColor = Color.white; // 还原颜色

            GUILayout.Space(10);
            EditorGUILayout.HelpBox("点击按钮可自动扫描项目中所有的 CommanderTemplateSO 并加入上方列表。", MessageType.Info);
        }

        private void CollectCommanders(CommanderSystemSettings settings)
        {
            if (settings.presetCommanders == null)
                settings.presetCommanders = new List<CommanderTemplateSO>();

            int oldCount = settings.presetCommanders.Count;
            
            settings.presetCommanders.Clear();

            string[] guids = AssetDatabase.FindAssets("t:CommanderTemplateSO");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CommanderTemplateSO template = AssetDatabase.LoadAssetAtPath<CommanderTemplateSO>(path);

                if (template != null)
                {
                    settings.presetCommanders.Add(template);
                }
            }

            EditorUtility.SetDirty(settings);
            // AssetDatabase.SaveAssets(); 

            Debug.Log($"<color=green><b>【名将录】</b> 更新完毕！</color>\n" +
                      $"旧数量: {oldCount}  ->  新数量: {settings.presetCommanders.Count}");
        }
    }
}