using System;
using System.Linq;
using TurnBasedGame.SpellCard;
using UnityEditor;
using UnityEngine;

namespace TurnBasedGame.SpellCard.Editor
{
    /// <summary>
    /// Custom PropertyDrawer cho [SpellEffectSelector].
    /// Hiển thị dropdown tất cả class implement ISpellEffect,
    /// khi chọn sẽ tạo instance và hiển thị inline params của effect đó.
    /// </summary>
    [CustomPropertyDrawer(typeof(SpellEffectSelectorAttribute))]
    public class SpellEffectSelectorDrawer : PropertyDrawer
    {
        private static Type[] _effectTypes;
        private static string[] _displayNames;

        static SpellEffectSelectorDrawer()
        {
            CacheEffectTypes();
        }

        private static void CacheEffectTypes()
        {
            _effectTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .Where(t => typeof(ISpellEffect).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .OrderBy(t => t.Name)
                .ToArray();

            _displayNames = new string[_effectTypes.Length + 1];
            _displayNames[0] = "(None)";
            for (int i = 0; i < _effectTypes.Length; i++)
                _displayNames[i + 1] = ObjectNames.NicifyVariableName(_effectTypes[i].Name);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Dropdown chọn effect type
            var currentType = property.managedReferenceValue?.GetType();
            int selected = 0;
            if (currentType != null)
            {
                int idx = Array.IndexOf(_effectTypes, currentType);
                if (idx >= 0) selected = idx + 1;
            }

            var dropdownRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginChangeCheck();
            int newSelected = EditorGUI.Popup(dropdownRect, label.text, selected, _displayNames);
            if (EditorGUI.EndChangeCheck() && newSelected != selected)
            {
                property.managedReferenceValue = newSelected > 0
                    ? Activator.CreateInstance(_effectTypes[newSelected - 1])
                    : null;
            }

            // Vẽ inline parameters của effect — dùng depth để dừng đúng thay vì GetEndProperty
            if (property.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                var copy = property.Copy();
                int baseDepth = copy.depth;

                if (copy.NextVisible(true))
                {
                    while (copy.depth > baseDepth)
                    {
                        float h = EditorGUI.GetPropertyHeight(copy, true);
                        EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), copy, true);
                        y += h + EditorGUIUtility.standardVerticalSpacing;
                        if (!copy.NextVisible(false)) break;
                    }
                }
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (property.managedReferenceValue != null)
            {
                var copy = property.Copy();
                int baseDepth = copy.depth;

                if (copy.NextVisible(true))
                {
                    while (copy.depth > baseDepth)
                    {
                        height += EditorGUI.GetPropertyHeight(copy, true) + EditorGUIUtility.standardVerticalSpacing;
                        if (!copy.NextVisible(false)) break;
                    }
                }
            }

            return height;
        }
    }
}
