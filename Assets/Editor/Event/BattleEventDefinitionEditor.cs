using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Linq;
using System.Collections.Generic;

namespace LongLiveKhioyen
{
    [CustomEditor(typeof(BattleEventDefinition))]
    public class BattleEventDefinitionEditor : Editor
    {
        private ReorderableList _actionList;
        private List<Type> _actionTypes;
        
        // 缓存当前选中的 Editor，避免每帧创建
        private Editor _currentActionEditor;
        private GameEventAction _currentSelectedAction;

        private void OnEnable()
        {
            // 1. 获取所有 GameEventAction 的子类
            _actionTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(GameEventAction)) && !t.IsAbstract)
                .ToList();

            // 2. 初始化列表
            _actionList = new ReorderableList(serializedObject, serializedObject.FindProperty("actions"), true, true, true, true);

            _actionList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Event Actions (Select to Edit)");
            };

            _actionList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = _actionList.serializedProperty.GetArrayElementAtIndex(index);
                var action = element.objectReferenceValue as GameEventAction;
                
                rect.y += 2;
                
                string label = "Empty Slot";
                if (action != null)
                {
                    string prefix = AssetDatabase.IsSubAsset(action) ? "[Inline] " : "[Ref] ";
                    label = prefix + (string.IsNullOrEmpty(action.name) ? action.GetType().Name : action.name);
                }
                
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element, new GUIContent(label));
            };

            _actionList.onSelectCallback = (ReorderableList l) =>
            {
                UpdateSelectedEditor(l);
            };

            _actionList.onAddDropdownCallback = (Rect buttonRect, ReorderableList l) =>
            {
                var menu = new GenericMenu();
                
                menu.AddItem(new GUIContent("Add Empty Slot (Reference)"), false, () => {
                    serializedObject.FindProperty("actions").InsertArrayElementAtIndex(serializedObject.FindProperty("actions").arraySize);
                    serializedObject.ApplyModifiedProperties();
                });

                menu.AddSeparator("");

                foreach (var type in _actionTypes)
                {
                    menu.AddItem(new GUIContent($"Create Inline/{type.Name}"), false, (t) => {
                        CreateInlineAction((Type)t);
                    }, type);
                }
                
                menu.ShowAsContext();
            };
            
            _actionList.onRemoveCallback = (ReorderableList l) =>
            {
                var element = l.serializedProperty.GetArrayElementAtIndex(l.index);
                var action = element.objectReferenceValue as GameEventAction;
                
                // 1. 如果是内嵌资源，销毁资源
                if (action != null && AssetDatabase.IsSubAsset(action))
                {
                    Undo.DestroyObjectImmediate(action);
                }
                
                // 2. 彻底移除列表元素
                // 技巧：先置空，再删除，或者直接操作 List
                // 对于 SerializedProperty，最稳健的方法是先设为 null，然后 Delete
                
                if (element.objectReferenceValue != null)
                {
                    element.objectReferenceValue = null;
                }
                l.serializedProperty.DeleteArrayElementAtIndex(l.index);
                
                // 清理编辑器缓存
                if (_currentActionEditor != null)
                {
                    DestroyImmediate(_currentActionEditor);
                    _currentActionEditor = null;
                    _currentSelectedAction = null;
                }
                
                serializedObject.ApplyModifiedProperties();
            };
        }

        private void UpdateSelectedEditor(ReorderableList l)
        {
            if (l.index < 0 || l.index >= l.serializedProperty.arraySize)
            {
                _currentSelectedAction = null;
                return;
            }

            var element = l.serializedProperty.GetArrayElementAtIndex(l.index);
            var action = element.objectReferenceValue as GameEventAction;

            if (action != _currentSelectedAction)
            {
                _currentSelectedAction = action;
                if (_currentActionEditor != null) DestroyImmediate(_currentActionEditor);
                
                if (action != null)
                {
                    _currentActionEditor = CreateEditor(action);
                }
            }
        }

        private void CreateInlineAction(Type type)
        {
            BattleEventDefinition def = (BattleEventDefinition)target;
            
            // 1. 创建实例
            GameEventAction newAction = (GameEventAction)ScriptableObject.CreateInstance(type);
            newAction.name = type.Name; 
            
            // 2. 添加到 Asset
            if (EditorUtility.IsPersistent(def))
            {
                AssetDatabase.AddObjectToAsset(newAction, def);
            }
            
            // 3. 添加到列表
            def.actions.Add(newAction);
            
            // 4. 保存
            EditorUtility.SetDirty(def);
            EditorUtility.SetDirty(newAction);
            AssetDatabase.SaveAssets();
            
            serializedObject.Update();
            
            // 自动选中新创建的
            _actionList.index = def.actions.Count - 1;
            UpdateSelectedEditor(_actionList);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            DrawPropertiesExcluding(serializedObject, "actions");
            
            GUILayout.Space(10);
            _actionList.DoLayoutList();
            
            // 绘制选中项的编辑器
            if (_currentSelectedAction != null && _currentActionEditor != null)
            {
                GUILayout.Space(10);
                GUILayout.Label($"Editing: {_currentSelectedAction.name}", EditorStyles.boldLabel);
                GUILayout.BeginVertical("box");
                
                // 改名功能
                if (AssetDatabase.IsSubAsset(_currentSelectedAction))
                {
                    EditorGUI.BeginChangeCheck();
                    string newName = EditorGUILayout.TextField("Action Name", _currentSelectedAction.name);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _currentSelectedAction.name = newName;
                        EditorUtility.SetDirty(_currentSelectedAction);
                        AssetDatabase.SaveAssets();
                    }
                    GUILayout.Space(5);
                }

                _currentActionEditor.OnInspectorGUI();
                
                GUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();
        }
        
        private void OnDisable()
        {
            if (_currentActionEditor != null) DestroyImmediate(_currentActionEditor);
        }
    }
}