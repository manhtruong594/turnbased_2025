using System.Collections.Generic;
using TurnBasedGame.Core;
using TurnBasedGame.Resources;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.SpellCard
{
    /// <summary>
    /// UI Panel hiển thị danh sách Spell Card khả dụng trong trận đấu.
    /// Sinh button từ hand, cập nhật trạng thái enable/disable theo MP và action.
    /// </summary>
    public class SpellCardPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private SpellCardBase _cardButtonPrefab;
        [SerializeField] private IntReference _actionLefts;

        private readonly List<SpellCardBase> _spells = new();
        private PlayerID _ownerPlayer;

        public void Initialize(IReadOnlyList<SpellCardData> spells, PlayerID owner)
        {
            _ownerPlayer = owner;

            // Đồng bộ hand với SpellCardManager
            SpellCardManager.Instance?.InitializeHand(owner, spells);

            if (GameMediator.Instance != null)
            {
                GameMediator.Instance.OnHandChanged += OnHandChanged;
                GameMediator.Instance.OnSpellCardUsed += OnCardUsed;
            }

            ClearButtons();
            foreach (var spell in spells)
            {
                if (spell == null) continue;
                var obj = Instantiate(_cardButtonPrefab, _cardContainer);
                if (obj != null)
                {
                    obj.Setup(spell, owner);
                    _spells.Add(obj);
                }
            }
            UpdateInteractable();
        }

        /// <summary>Overload giữ tương thích ngược, mặc định Player1.</summary>
        public void Initialize(IReadOnlyList<SpellCardData> spells)
        {
            Initialize(spells, PlayerID.Player1);
        }

        private void OnDestroy()
        {
            if (GameMediator.Instance != null)
            {
                GameMediator.Instance.OnHandChanged -= OnHandChanged;
                GameMediator.Instance.OnSpellCardUsed -= OnCardUsed;
            }
        }

        private void OnHandChanged(PlayerID player)
        {
            if (player != _ownerPlayer) return;
            RebuildFromHand();
        }

        private void OnCardUsed(SpellCardData card, PlayerID caster)
        {
            UpdateInteractable();
        }

        public void UpdateInteractable()
        {
            foreach (var btn in _spells)
            {
                if (btn == null || btn.Data == null) continue;
                bool canUse = SpellCardManager.Instance != null
                    && SpellCardManager.Instance.CanUseCard(btn.Data, _ownerPlayer);
                btn.SetInteractable(canUse);
            }
        }

        private void RebuildFromHand()
        {
            ClearButtons();
            var hand = SpellCardManager.Instance?.GetHand(_ownerPlayer);
            if (hand == null) return;

            foreach (var spell in hand)
            {
                if (spell == null) continue;
                var obj = Instantiate(_cardButtonPrefab, _cardContainer);
                if (obj != null)
                {
                    obj.Setup(spell, _ownerPlayer);
                    _spells.Add(obj);
                }
            }
            UpdateInteractable();
        }

        private void ClearButtons()
        {
            foreach (var btn in _spells)
            {
                if (btn != null) Destroy(btn.gameObject);
            }
            _spells.Clear();
        }
    }
}
