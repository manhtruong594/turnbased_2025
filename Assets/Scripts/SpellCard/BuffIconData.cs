using System;
using System.Collections.Generic;
using TurnBasedGame.EditorSupport;
using UnityEngine;

namespace TurnBasedGame.SpellCard
{
    [CreateAssetMenu(fileName = "BuffIconData", menuName = "TurnBased/Buff Icon Data")]
    public class BuffIconData : ScriptableObject
    {
        [SerializeField] private List<Entry> _entries = new();

        public Sprite GetIcon(StatusEffectType type)
        {
            foreach (var e in _entries)
                if (e.type == type) return e.icon;
            return null;
        }

        [Serializable]
        public struct Entry
        {
            public StatusEffectType type;
            [SpritePreview]
            public Sprite icon;
        }
    }
}
