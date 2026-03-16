// Assets/Editor/Battle/ActionConditionDrawer.cs

using UnityEngine;
using UnityEditor;
using LongLiveKhioyen;

[CustomPropertyDrawer(typeof(ActionCondition))]
public class ActionConditionDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 1. 开始绘制属性
        EditorGUI.BeginProperty(position, label, property);
        
        // 2. 绘制一个带标题的折叠框，保持整洁
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.isExpanded, label);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            // 获取所需的 SerializedProperty
            SerializedProperty categoryProp = property.FindPropertyRelative("category");
            SerializedProperty operandAProp = property.FindPropertyRelative("operandA");
            SerializedProperty compareOpProp = property.FindPropertyRelative("compareOp");
            SerializedProperty operandBProp = property.FindPropertyRelative("operandB");
            SerializedProperty targetTagProp = property.FindPropertyRelative("targetTag");

            // 绘制类别选择器
            Rect categoryRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(categoryRect, categoryProp);

            ConditionCategory currentCategory = (ConditionCategory)categoryProp.enumValueIndex;

            // 根据类别决定绘制哪些字段
            if (currentCategory == ConditionCategory.NumericalComparison)
            {
                // 计算位置
                Rect opARect = new Rect(position.x, categoryRect.yMax + 2, position.width, EditorGUI.GetPropertyHeight(operandAProp));
                Rect compRect = new Rect(position.x, opARect.yMax + 2, position.width, EditorGUIUtility.singleLineHeight);
                Rect opBRect = new Rect(position.x, compRect.yMax + 2, position.width, EditorGUI.GetPropertyHeight(operandBProp));

                // 绘制数值比较相关的字段
                EditorGUI.PropertyField(opARect, operandAProp, true); // true 表示绘制子属性
                EditorGUI.PropertyField(compRect, compareOpProp);
                EditorGUI.PropertyField(opBRect, operandBProp, true);
            }
            else if (currentCategory == ConditionCategory.CheckExternalTag)
            {
                // 绘制 Tag 输入框
                Rect tagRect = new Rect(position.x, categoryRect.yMax + 2, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(tagRect, targetTagProp);
            }

            EditorGUI.indentLevel--;
        }

        // 3. 结束绘制
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        // 基础高度 (Foldout + Category 字段)
        float height = EditorGUIUtility.singleLineHeight * 2 + 4;

        SerializedProperty categoryProp = property.FindPropertyRelative("category");
        ConditionCategory currentCategory = (ConditionCategory)categoryProp.enumValueIndex;

        if (currentCategory == ConditionCategory.NumericalComparison)
        {
            SerializedProperty operandAProp = property.FindPropertyRelative("operandA");
            SerializedProperty operandBProp = property.FindPropertyRelative("operandB");
            
            // 加上 operandA, compareOp, operandB 的高度
            height += EditorGUI.GetPropertyHeight(operandAProp, true) + 2;
            height += EditorGUIUtility.singleLineHeight + 2;
            height += EditorGUI.GetPropertyHeight(operandBProp, true) + 2;
        }
        else if (currentCategory == ConditionCategory.CheckExternalTag)
        {
            // 加上 targetTag 的高度
            height += EditorGUIUtility.singleLineHeight + 2;
        }

        return height;
    }
}