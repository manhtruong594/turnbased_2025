using UnityEngine;

namespace TurnBasedGame.EditorSupport
{
    public sealed class SpritePreviewAttribute : PropertyAttribute
    {
        public const float DefaultHeight = 96f;

        public SpritePreviewAttribute(float height = DefaultHeight)
        {
            Height = Mathf.Max(MinimumHeight, height);
        }

        public float Height { get; }

        private const float MinimumHeight = 18f;
    }
}
