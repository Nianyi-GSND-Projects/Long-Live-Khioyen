using System.Collections;
using System.Collections.Generic;
using LongLiveKhioyen;
using UnityEngine;
using UnityEditor;
namespace LongLiveKhioyen
{
    
    [CustomEditor(typeof(ActionDatabase))]
    public class ActionDatabaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            ActionDatabase sheet = (ActionDatabase)target;

            GUILayout.Space(10);
            
            if(GUILayout.Button("我要收集所有的行动数据",GUILayout.Height(40))) sheet.GenerateActionDefinitionMap();
            
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("使用此按钮以将项目中的所有行动数据加入ActionDefinitionSheet", MessageType.Info);
        }
    }
}
