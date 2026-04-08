using System.Collections.Generic;
using TurnBasedGame.ObjectPool;
using UnityEngine;

namespace TurnBasedGame.SpellCard
{
    /// <summary>
    /// UI hiển thị danh sách icon buff/debuff trên đầu unit.
    /// Tự đồng bộ khi effect thêm/xoá, cập nhật số lượt còn lại mỗi turn.
    /// </summary>
    public class BuffIconDisplay : MonoBehaviour
    {
        [SerializeField] private Transform _iconContainer;
        [SerializeField] private BuffIconEntry _buffIconPrefab;
        [SerializeField] private BuffIconData _iconData;

        private readonly Dictionary<StatusEffectType, BuffIconEntry> _activeIcons = new();

        public void Init()
        {
        }

        public void OnEffectAdded(ActiveStatusEffect effect)
        {
            if (_activeIcons.ContainsKey(effect.Type)) return;
            CreateIcon(effect);
        }

        public void OnEffectRemoved(ActiveStatusEffect effect)
        {
            if (_activeIcons.TryGetValue(effect.Type, out var entry))
            {
                ObjectPoolManager.Instance.Despawn(entry);
                _activeIcons.Remove(effect.Type);
            }
        }

        private void CreateIcon(ActiveStatusEffect effect)
        {
            if (_buffIconPrefab == null || _iconContainer == null || _iconData == null) return;
            var sprite = _iconData.GetIcon(effect.Type);
            if (sprite == null) return;
            var iconObj = ObjectPoolManager.Instance.Spawn(_buffIconPrefab.gameObject, _iconContainer);
            var entry = iconObj.GetComponent<BuffIconEntry>();
            if (entry != null)
            {
                entry.Setup(sprite, effect.RemainingTurns, _iconData.GetColor(effect.Type));
                _activeIcons[effect.Type] = entry;
            }
        }
    }
}
