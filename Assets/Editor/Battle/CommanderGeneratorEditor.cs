using UnityEngine;
using UnityEditor;
using LongLiveKhioyen;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    [CustomEditor(typeof(CommanderGeneratorSO))]
    public class CommanderGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            CommanderGeneratorSO generator = (CommanderGeneratorSO)target;

            GUILayout.Space(10);
            if (GUILayout.Button("Collect All Rules", GUILayout.Height(30)))
            {
                CollectRules(generator);
            }
        }

        private void CollectRules(CommanderGeneratorSO generator)
        {
            // 1. 收集 Identity Rules
            generator.identityRules = FindAssets<CommanderIdentityRuleSO>();
            Debug.Log($"Collected {generator.identityRules.Count} Identity Rules.");

            // 2. 收集 Stats Rules
            generator.statsRules = FindAssets<CommanderStatsRuleSO>();
            Debug.Log($"Collected {generator.statsRules.Count} Stats Rules.");

            // 3. 收集 Traits Rules
            generator.traitsRules = FindAssets<CommanderTraitsRuleSO>();
            Debug.Log($"Collected {generator.traitsRules.Count} Traits Rules.");

            // 4. 保存更改
            EditorUtility.SetDirty(generator);
            AssetDatabase.SaveAssets();
        }

        private List<T> FindAssets<T>() where T : ScriptableObject
        {
            List<T> assets = new List<T>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }
            
            // 可选：按名称排序，保证列表顺序稳定
            assets.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            
            return assets;
        }
    }
}