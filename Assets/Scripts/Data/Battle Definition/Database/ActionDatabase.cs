using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LongLiveKhioyen
{
    [CreateAssetMenu(menuName = "Long Live Khioyen/Battle/Database/Action Database")]
    public class ActionDatabase : ScriptableObject
    {
        [SerializeField] List<ActionDefinition> actionDefinitions = new List<ActionDefinition>();
        
        private Dictionary<string, ActionDefinition> actionDefinitionMap;

        public void Initialize()
        {
            actionDefinitionMap = new Dictionary<string, ActionDefinition>();
            foreach (var actionDefinition in actionDefinitions)
            {
                if(actionDefinition !=null && !actionDefinitionMap.ContainsKey(actionDefinition.actionName))
                actionDefinitionMap.Add(actionDefinition.actionName, actionDefinition);
            }
            
            Debug.Log($"Action Definition Sheet initialized with {actionDefinitionMap.Count} actions.");
        }

        public ActionDefinition GetAction(string actionName)
        {
            if(actionDefinitionMap == null)
                Initialize();

            if (actionDefinitionMap.TryGetValue(actionName, out var actionDefinition))
            {
                return actionDefinition;
            }
            Debug.LogError($"Action Definition not found for {actionName}");
            return null;
        }
        #if UNITY_EDITOR
        [ContextMenu("Generate Action Definition Map")]

        public void GenerateActionDefinitionMap()
        {
            actionDefinitions.Clear();
            
            string[] guids = AssetDatabase.FindAssets("t:ActionDefinition");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ActionDefinition actionDefinition = AssetDatabase.LoadAssetAtPath<ActionDefinition>(path);
                
                if(actionDefinition !=null &&!actionDefinitions.Contains(actionDefinition))
                actionDefinitions.Add(actionDefinition);
            }
            Debug.Log($"Auto-populated {actionDefinitions.Count} actions into the database.");
            
            EditorUtility.SetDirty(this);
        }
        #endif
    }
}
