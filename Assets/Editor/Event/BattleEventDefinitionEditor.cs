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
        private List<Type> _conditionTypes;
        private Editor _currentActionEditor;
        private GameEventAction _currentSelectedAction;

        private void OnEnable()
        {
            // 1. 获取所有 GameEventAction 的子类
            _actionTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(GameEventAction)) && !t.IsAbstract)
                .ToList();
            
            _conditionTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(BattleEventCondition)) && !t.IsAbstract)
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
            
            DrawPropertiesExcluding(serializedObject, "actions", "conditionGroups");
            
            GUILayout.Space(10);
            DrawConditionGroups();
            
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
        
        private void DrawConditionGroups()
        {
            SerializedProperty groupsProp = serializedObject.FindProperty("conditionGroups");
            
            EditorGUILayout.LabelField("Trigger Conditions (OR logic between groups)", EditorStyles.boldLabel);
            
            for (int i = 0; i < groupsProp.arraySize; i++)
            {
                EditorGUILayout.BeginVertical("helpbox");
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Condition Group {i} (AND logic inside)", EditorStyles.miniBoldLabel);
                
                // 删除组按钮
                if (GUILayout.Button("Remove Group", EditorStyles.miniButton, GUILayout.Width(100)))
                {
                    groupsProp.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break; // 打断循环，下一帧重新绘制
                }
                EditorGUILayout.EndHorizontal();

                SerializedProperty groupProp = groupsProp.GetArrayElementAtIndex(i);
                SerializedProperty conditionsProp = groupProp.FindPropertyRelative("conditions");

                // 遍历组内的所有具体条件
                for (int j = 0; j < conditionsProp.arraySize; j++)
                {
                    SerializedProperty conditionProp = conditionsProp.GetArrayElementAtIndex(j);
                    
                    EditorGUILayout.BeginHorizontal("box");
                    
                    // 判断这个 SerializeReference 是否为空
                    if (string.IsNullOrEmpty(conditionProp.managedReferenceFullTypename))
                    {
                        EditorGUILayout.LabelField("Empty Condition", GUILayout.Width(120));
                        if (GUILayout.Button("Select Type...", GUILayout.Width(120)))
                        {
                            ShowConditionTypeMenu(conditionProp.propertyPath);
                        }
                    }
                    else
                    {
                        // 提取纯类名作为标签
                        string typeName = conditionProp.managedReferenceFullTypename.Split(' ').Last().Split('.').Last();
                        // 净化类名（比如把 Condition_TurnCountEquals 变成 TurnCountEquals）
                        typeName = typeName.Replace("Condition_", "");
                        
                        EditorGUILayout.LabelField(typeName, EditorStyles.boldLabel, GUILayout.Width(150));
                        
                        // 核心：使用 PropertyField 绘制这个多态对象里面的全部字段！
                        EditorGUILayout.PropertyField(conditionProp, GUIContent.none, true);
                    }

                    // 删除单条条件
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        // Unity 删除 SerializeReference 的安全做法：先置空，再删除
                        int oldSize = conditionsProp.arraySize;
                        conditionsProp.DeleteArrayElementAtIndex(j);
                        if (conditionsProp.arraySize == oldSize)
                        {
                            // 如果尺寸没变，说明第一次只是置空了引用，需要再删一次彻底移除元素
                            conditionsProp.DeleteArrayElementAtIndex(j);
                        }
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                // 添加条件按钮
                if (GUILayout.Button("+ Add Condition", EditorStyles.miniButton))
                {
                    conditionsProp.arraySize++;
                    SerializedProperty newCond = conditionsProp.GetArrayElementAtIndex(conditionsProp.arraySize - 1);
                    newCond.managedReferenceValue = null; // 确保新元素为空
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }

            if (GUILayout.Button("Add New Condition Group", GUILayout.Height(25)))
            {
                groupsProp.arraySize++;
                SerializedProperty newGroup = groupsProp.GetArrayElementAtIndex(groupsProp.arraySize - 1);
                // 新建组时，把内部的 conditions 列表清空，防止复制了上一个组的数据
                newGroup.FindPropertyRelative("conditions").ClearArray();
            }
        }
        
        private void ShowConditionTypeMenu(string propertyPath)
        {
            GenericMenu menu = new GenericMenu();
            foreach (var type in _conditionTypes)
            {
                // 利用下划线分级，比如 Condition_TurnCount 会变成菜单里的 Condition/TurnCount
                string menuPath = type.Name.Replace("_", "/");
                
                menu.AddItem(new GUIContent(menuPath), false, () => 
                {
                    serializedObject.Update();
                    SerializedProperty prop = serializedObject.FindProperty(propertyPath);
                    if (prop != null)
                    {
                        // 实例化多态子类并赋值给 SerializeReference
                        object instance = Activator.CreateInstance(type);
                        prop.managedReferenceValue = instance;
                        serializedObject.ApplyModifiedProperties();
                    }
                });
            }
            menu.ShowAsContext();
        }
        
        private void OnDisable()
        {
            if (_currentActionEditor != null) DestroyImmediate(_currentActionEditor);
        }
    }
}