// Assets/Editor/Item/EquipmentDefinitionEditor.cs

using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using LongLiveKhioyen;

[CustomEditor(typeof(EquipmentDefinition))]
public class EquipmentDefinitionEditor : Editor
{
    private EquipmentDefinition _target;
    private List<Type> _effectTypes;

    private void OnEnable()
    {
        _target = (EquipmentDefinition)target;
        _effectTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsSubclassOf(typeof(EquipmentEffect)) && !t.IsAbstract)
            .ToList();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Equipment Effects (Sub-Assets)", EditorStyles.boldLabel);

        if (_target.effects == null) _target.effects = new List<EquipmentEffect>();

        for (int i = 0; i < _target.effects.Count; i++)
        {
            var effect = _target.effects[i];
            if (effect == null) continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            effect.name = EditorGUILayout.TextField("Name", effect.name);
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                RemoveEffect(effect);
                i--; // 调整索引
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                continue;
            }
            EditorGUILayout.EndHorizontal();

            Editor effectEditor = CreateEditor(effect);
            effectEditor.OnInspectorGUI();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        if (GUILayout.Button("Add New Effect"))
        {
            ShowAddEffectMenu();
        }
    }

    private void ShowAddEffectMenu()
    {
        GenericMenu menu = new GenericMenu();
        foreach (var type in _effectTypes)
        {
            menu.AddItem(new GUIContent(type.Name), false, () => AddEffect(type));
        }
        menu.ShowAsContext();
    }

    private void AddEffect(Type type)
    {
        EquipmentEffect newEffect = (EquipmentEffect)ScriptableObject.CreateInstance(type);
        newEffect.name = type.Name;
        
        AssetDatabase.AddObjectToAsset(newEffect, _target);
        
        _target.effects.Add(newEffect);

        EditorUtility.SetDirty(_target);
        AssetDatabase.SaveAssets();
    }

    private void RemoveEffect(EquipmentEffect effect)
    {
        _target.effects.Remove(effect);

        DestroyImmediate(effect, true); // true = allowDestroyingAssets

        EditorUtility.SetDirty(_target);
        AssetDatabase.SaveAssets();
    }
}