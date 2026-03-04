// Assets/Editor/ActionDefinitionEditor.cs (混合模式版本)

using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using LongLiveKhioyen;

[CustomEditor(typeof(ActionDefinition))]
public class ActionDefinitionEditor : Editor
{
    private ActionDefinition _targetAction;
    private SerializedProperty _effectsListProperty;
    private List<Type> _effectTypes;
    private List<Type> _buffTypes;

    private void OnEnable()
    {
        _targetAction = (ActionDefinition)target;
        _effectsListProperty = serializedObject.FindProperty("effects");
        
        var allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes());
        _effectTypes = allTypes.Where(t => t.IsSubclassOf(typeof(EffectDefinition)) && !t.IsAbstract).ToList();
        _buffTypes = allTypes.Where(t => t.IsSubclassOf(typeof(BuffDefinition)) && !t.IsAbstract).ToList();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "effects");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Action Effects", EditorStyles.boldLabel);

        // 使用 ReorderableList 绘制效果列表，体验更好
        // 这里为了简单，我们继续用 for 循环
        for (int i = 0; i < _effectsListProperty.arraySize; i++)
        {
            SerializedProperty effectProperty = _effectsListProperty.GetArrayElementAtIndex(i);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // 头部：对象引用字段和移除按钮
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(effectProperty, new GUIContent("Effect " + i));
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                // 如果是子资产，移除它
                var objToRemove = effectProperty.objectReferenceValue;
                if (objToRemove != null && AssetDatabase.IsSubAsset(objToRemove))
                {
                    DestroyImmediate(objToRemove, true);
                }
                _effectsListProperty.DeleteArrayElementAtIndex(i);
                i--;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                continue;
            }
            EditorGUILayout.EndHorizontal();

            // 如果有引用，则绘制内联属性
            if (effectProperty.objectReferenceValue != null)
            {
                var effectObject = effectProperty.objectReferenceValue;
                var effectEditor = CreateEditor(effectObject);
                
                // 绘制一个可折叠的区域
                effectProperty.isExpanded = EditorGUILayout.Foldout(effectProperty.isExpanded, "Details", true);
                if (effectProperty.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    effectEditor.OnInspectorGUI();
                    
                    // 特殊处理 AddBuffEffect
                    if (effectObject is AddBuffEffect addBuffEffect)
                    {
                        DrawAddBuffEffectGUI(addBuffEffect);
                    }
                    EditorGUI.indentLevel--;
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        // 添加新效果的按钮
        if (GUILayout.Button("Add New Inline Effect"))
        {
            ShowAddEffectMenu();
        }
        if (GUILayout.Button("Add Existing Effect Slot"))
        {
            _effectsListProperty.arraySize++;
        }

        serializedObject.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }

    private void DrawAddBuffEffectGUI(AddBuffEffect addBuffEffect)
    {
        EditorGUILayout.Space();
        
        // 使用 SerializedObject 来处理撤销和保存
        var addBuffEffectSO = new SerializedObject(addBuffEffect);
        var buffDefProperty = addBuffEffectSO.FindProperty("buffDefinition");

        // 1. 绘制标准的 Buff Definition 引用字段
        EditorGUILayout.PropertyField(buffDefProperty, new GUIContent("Buff Definition"));

        // 2. 如果引用为空，显示创建按钮
        if (buffDefProperty.objectReferenceValue == null)
        {
            if (GUILayout.Button("Create Inline Buff"))
            {
                ShowAddBuffMenu(addBuffEffect);
            }
        }
        else
        {
            // 3. 如果有引用，绘制其内联属性
            var buffObject = buffDefProperty.objectReferenceValue;
            var buffEditor = CreateEditor(buffObject);
            
            buffDefProperty.isExpanded = EditorGUILayout.Foldout(buffDefProperty.isExpanded, "Buff Details", true);
            if (buffDefProperty.isExpanded)
            {
                EditorGUI.indentLevel++;
                buffEditor.OnInspectorGUI();
                EditorGUI.indentLevel--;
            }
        }
        
        addBuffEffectSO.ApplyModifiedProperties();
    }

    private void ShowAddEffectMenu()
    {
        GenericMenu menu = new GenericMenu();
        foreach (var type in _effectTypes)
        {
            menu.AddItem(new GUIContent("Create " + type.Name), false, () => AddEffect(type));
        }
        menu.ShowAsContext();
    }

    private void ShowAddBuffMenu(AddBuffEffect ownerEffect)
    {
        GenericMenu menu = new GenericMenu();
        foreach (var type in _buffTypes)
        {
            menu.AddItem(new GUIContent("Create " + type.Name), false, () => AddBuffToEffect(ownerEffect, type));
        }
        menu.ShowAsContext();
    }

    private void AddEffect(Type type)
    {
        _effectsListProperty.arraySize++;
        var newEffectProperty = _effectsListProperty.GetArrayElementAtIndex(_effectsListProperty.arraySize - 1);

        var newEffect = (EffectDefinition)CreateInstance(type);
        newEffect.name = type.Name;
        
        AssetDatabase.AddObjectToAsset(newEffect, _targetAction);
        
        newEffectProperty.objectReferenceValue = newEffect;
        
        serializedObject.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void AddBuffToEffect(AddBuffEffect ownerEffect, Type type)
    {
        var addBuffEffectSO = new SerializedObject(ownerEffect);
        var buffDefProperty = addBuffEffectSO.FindProperty("buffDefinition");

        var newBuff = (BuffDefinition)CreateInstance(type);
        newBuff.name = type.Name;

        AssetDatabase.AddObjectToAsset(newBuff, _targetAction);
        
        buffDefProperty.objectReferenceValue = newBuff;
        
        addBuffEffectSO.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    
    // 移除子资产的逻辑现在在 OnInspectorGUI 中处理
}