using TurnBasedGame.EditorSupport;
using UnityEditor;
using UnityEngine;

namespace TurnBasedGame.Editor
{
    [CustomPropertyDrawer(typeof(SpritePreviewAttribute))]
    public sealed class SpritePreviewDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference ||
                property.hasMultipleDifferentValues ||
                property.objectReferenceValue == null)
                return EditorGUIUtility.singleLineHeight;

            var previewAttribute = (SpritePreviewAttribute)attribute;
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + previewAttribute.Height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var previousShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;

            EditorGUI.BeginChangeCheck();
            var sprite = (Sprite)EditorGUI.ObjectField(
                fieldRect,
                label,
                property.objectReferenceValue,
                typeof(Sprite),
                false);
            if (EditorGUI.EndChangeCheck())
                property.objectReferenceValue = sprite;

            EditorGUI.showMixedValue = previousShowMixedValue;

            if (!property.hasMultipleDifferentValues && sprite != null)
            {
                var previewAttribute = (SpritePreviewAttribute)attribute;
                var previewRect = new Rect(
                    position.x,
                    fieldRect.yMax + EditorGUIUtility.standardVerticalSpacing,
                    position.width,
                    previewAttribute.Height);

                DrawPreview(previewRect, sprite);
            }

            EditorGUI.EndProperty();
        }

        private static void DrawPreview(Rect rect, Sprite sprite)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);

            var texture = AssetPreview.GetAssetPreview(sprite) ?? AssetPreview.GetMiniThumbnail(sprite);
            if (texture == null)
                return;

            const float padding = 4f;
            var contentRect = new Rect(
                rect.x + padding,
                rect.y + padding,
                rect.width - padding * 2f,
                rect.height - padding * 2f);

            GUI.DrawTexture(contentRect, texture, ScaleMode.ScaleToFit, true);
        }
    }
}
