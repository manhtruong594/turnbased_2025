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

        private readonly Dictionary<SpellEffectType, GameObject> _activeIcons = new();

        private void OnEnable()
        {
            if (_handler == null) _handler = GetComponentInParent<BuffDebuffHandler>();
            if (_handler != null)
            {
                _handler.OnBuffAdded += OnBuffAdded;
                _handler.OnBuffRemoved += OnBuffRemoved;
            }
        }

        private void OnDisable()
        {
            if (_handler != null)
            {
                _handler.OnBuffAdded -= OnBuffAdded;
                _handler.OnBuffRemoved -= OnBuffRemoved;
            }
        }

        private void OnBuffAdded(ActiveBuff buff)
        {
            if (_activeIcons.ContainsKey(buff.Type)) return;
            if (_buffIconPrefab == null || _iconContainer == null) return;

            var iconObj = Instantiate(_buffIconPrefab, _iconContainer);
            var image = iconObj.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = GetIconForType(buff.Type);
            }
            _activeIcons[buff.Type] = iconObj;
        }

        private void OnBuffRemoved(ActiveBuff buff)
        {
            if (_activeIcons.TryGetValue(buff.Type, out var iconObj))
            {
                Destroy(iconObj);
                _activeIcons.Remove(buff.Type);
            }
        }

        private Sprite GetIconForType(SpellEffectType type)
        {
            return type switch
            {
                SpellEffectType.Shield => shieldIcon,
                SpellEffectType.Root => rootIcon,
                SpellEffectType.Heal => healIcon,
                SpellEffectType.DamageBuff => damageBuffIcon,
                _ => null
            };
        }
    }
}
