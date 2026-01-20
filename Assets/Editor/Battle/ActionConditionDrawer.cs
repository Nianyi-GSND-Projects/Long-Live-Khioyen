using UnityEngine;
using UnityEditor;

namespace LongLiveKhioyen
{
    [CustomPropertyDrawer(typeof(ActionCondition))]
    public class ActionConditionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // 获取子属性
            var opA = property.FindPropertyRelative("operandA");
            var compare = property.FindPropertyRelative("compareOp");
            var opB = property.FindPropertyRelative("operandB");

            // 计算布局
            float totalWidth = position.width;
            float opWidth = 40f; // 运算符宽度
            float operandWidth = (totalWidth - opWidth) / 2f;

            Rect rectA = new Rect(position.x, position.y, operandWidth, position.height);
            Rect rectOp = new Rect(position.x + operandWidth, position.y, opWidth, position.height);
            Rect rectB = new Rect(position.x + operandWidth + opWidth, position.y, operandWidth, position.height);

            // 绘制 A
            DrawOperand(rectA, opA);
            
            // 绘制 运算符
            // 简单的显示为 Enum Popup
            // 这里为了美观，可以用 EditorGUI.PropertyField，也可以自定义显示符号
            EditorGUI.PropertyField(rectOp, compare, GUIContent.none);

            // 绘制 B
            DrawOperand(rectB, opB);

            EditorGUI.EndProperty();
        }

        private void DrawOperand(Rect rect, SerializedProperty operandProp)
        {
            var sourceType = operandProp.FindPropertyRelative("sourceType");
            var constValue = operandProp.FindPropertyRelative("constantValue");

            // 获取当前选中的枚举索引
            int enumIndex = sourceType.enumValueIndex;
            // 假设 0 是 Constant (需要跟 Enum 定义对应)
            bool isConstant = (enumIndex == 0); 

            if (isConstant)
            {
                // 如果是常量，分两半：左边选类型，右边填数值
                float split = rect.width * 0.5f;
                Rect rectType = new Rect(rect.x, rect.y, split, rect.height);
                Rect rectVal = new Rect(rect.x + split, rect.y, split, rect.height);

                EditorGUI.PropertyField(rectType, sourceType, GUIContent.none);
                EditorGUI.PropertyField(rectVal, constValue, GUIContent.none);
            }
            else
            {
                // 如果是变量，只显示类型选择
                EditorGUI.PropertyField(rect, sourceType, GUIContent.none);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}