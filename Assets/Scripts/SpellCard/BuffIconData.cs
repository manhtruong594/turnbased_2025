using System;
using System.Collections.Generic;
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

        public Color GetColor(StatusEffectType type)
        {
            foreach (var e in _entries)
                if (e.type == type) return e.color;
            return Color.white;
        }

        [Serializable]
        public struct Entry
        {
            public StatusEffectType type;
            public Sprite icon;
            public Color color;
        }
    }
}