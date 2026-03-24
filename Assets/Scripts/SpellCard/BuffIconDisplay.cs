using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TurnBasedGame.SpellCard
{
    /// <summary>
    /// UI hiển thị icon buff/debuff trên đầu unit.
    /// Tự động cập nhật khi buff thay đổi.
    /// </summary>
    public class BuffIconDisplay : MonoBehaviour
    {
        [SerializeField] private Transform _iconContainer;
        [SerializeField] private GameObject _buffIconPrefab;
        [SerializeField] private BuffDebuffHandler _handler;

        [Header("Icons theo loại")]
        [SerializeField] private Sprite shieldIcon;
        [SerializeField] private Sprite rootIcon;
        [SerializeField] private Sprite healIcon;
        [SerializeField] private Sprite damageBuffIcon;

        private readonly Dictionary<StatusEffectType, GameObject> _activeIcons = new();

        private void OnEnable()
        {
            if (_handler == null) _handler = GetComponentInParent<BuffDebuffHandler>();
            if (_handler != null)
            {
                _handler.OnEffectAdded += OnEffectAdded;
                _handler.OnEffectRemoved += OnEffectRemoved;
            }
        }

        private void OnDisable()
        {
            if (_handler != null)
            {
                _handler.OnEffectAdded -= OnEffectAdded;
                _handler.OnEffectRemoved -= OnEffectRemoved;
            }
        }

        private void OnEffectAdded(ActiveStatusEffect effect)
        {
            if (_activeIcons.ContainsKey(effect.Type)) return;
            if (_buffIconPrefab == null || _iconContainer == null) return;

            var iconObj = Instantiate(_buffIconPrefab, _iconContainer);
            var image = iconObj.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = GetIconForType(effect.Type);
            }
            _activeIcons[effect.Type] = iconObj;
        }

        private void OnEffectRemoved(ActiveStatusEffect effect)
        {
            if (_activeIcons.TryGetValue(effect.Type, out var iconObj))
            {
                Destroy(iconObj);
                _activeIcons.Remove(effect.Type);
            }
        }

        private Sprite GetIconForType(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Shield => shieldIcon,
                StatusEffectType.Heal => healIcon,
                StatusEffectType.DamageBuff => damageBuffIcon,
                StatusEffectType.Root => rootIcon,
                _ => null
            };
        }
    }
}
