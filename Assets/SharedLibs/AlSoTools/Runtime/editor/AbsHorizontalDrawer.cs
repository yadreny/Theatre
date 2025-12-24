#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AlSo.PropertyDrawers
{

    public abstract class AbsHorizontalDrawer<T> : PropertyDrawer where T : class
    {
        private List<FieldInfo> _visibleFields;

        private void CacheVisibleFields()
        {
            if (_visibleFields != null) return;

            _visibleFields = new List<FieldInfo>();
            var fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                if ((field.IsPublic || field.GetCustomAttribute<SerializeField>() != null) &&
                    field.GetCustomAttribute<HideInInspector>() == null)
                {
                    _visibleFields.Add(field);
                }
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            CacheVisibleFields();

            EditorGUI.BeginProperty(position, label, property);
            int originalIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float spacing = 4f;
            float height = EditorGUIUtility.singleLineHeight;

            // Step 1: Calculate total width usage
            float totalWidth = position.width;
            float checkboxWidth = 18f;

            float remainingWidth = totalWidth;
            int expandableCount = 0;
            Dictionary<string, float> fieldWidths = new();

            foreach (var field in _visibleFields)
            {
                float fieldWidth;
                if (field.FieldType == typeof(bool))
                {
                    fieldWidth = checkboxWidth;
                }
                else
                {
                    expandableCount++;
                    fieldWidth = -1f; // to be distributed later
                }

                fieldWidths[field.Name] = fieldWidth;
                if (fieldWidth > 0)
                    remainingWidth -= fieldWidth;
            }

            // Distribute remaining width evenly among expandable fields
            float autoWidth = (expandableCount > 0) ? (remainingWidth - spacing * (_visibleFields.Count - 1)) / expandableCount : 0f;

            float x = position.x;
            foreach (var field in _visibleFields)
            {
                var subProp = property.FindPropertyRelative(field.Name);
                if (subProp == null) continue;

                float width = fieldWidths[field.Name] > 0 ? fieldWidths[field.Name] : autoWidth;
                Rect fieldRect = new Rect(x, position.y, width, height);
                EditorGUI.PropertyField(fieldRect, subProp, GUIContent.none);

                x += width + spacing;
            }

            EditorGUI.indentLevel = originalIndent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
#endif