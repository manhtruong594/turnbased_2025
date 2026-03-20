using System;
using System.Collections.Generic;
using RedBjorn.ProtoTiles.Example;
using TurnBasedGame.Core;
using TurnBasedGame.Resources;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.SpellCard
{
    /// <summary>
    /// Runtime manager xử lý toàn bộ logic sử dụng Spell Card trong trận đấu.
    /// Quản lý hand (bộ spell khả dụng), validation target, tiêu hao MP, kích hoạt effect.
    /// </summary>
    public class SpellCardManager : BaseManager
    {
        public static SpellCardManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private IntReference _actionLefts;

        private readonly Dictionary<PlayerID, List<SpellCardData>> _playerHands = new();
        private SpellCardData _selectedCard;
        private PlayerID _currentCaster;
        private bool _isTargeting;

        // Events cho UI binding
        public event Action<SpellCardData> OnCardSelected;
        public event Action OnCardDeselected;
        public event Action<SpellCardData, UnitController> OnCardUsed;
        public event Action<PlayerID> OnHandChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Khởi tạo hand cho player từ danh sách spell đã chọn trước trận.
        /// Gọi khi trận đấu bắt đầu.
        /// </summary>
        public void InitializeHand(PlayerID player, IReadOnlyList<SpellCardData> spells)
        {
            var hand = new List<SpellCardData>();
            if (spells != null)
            {
                foreach (var spell in spells)
                {
                    if (spell != null) hand.Add(spell);
                }
            }
            _playerHands[player] = hand;
            OnHandChanged?.Invoke(player);
        }

        public IReadOnlyList<SpellCardData> GetHand(PlayerID player)
        {
            return _playerHands.TryGetValue(player, out var hand) ? hand : Array.Empty<SpellCardData>();
        }

        /// <summary>
        /// Player chọn 1 spell card từ UI => bắt đầu chế độ chọn target.
        /// </summary>
        public void SelectCard(SpellCardData card, PlayerID caster)
        {
            if (card == null) return;
            if (!CanUseCard(card, caster))
            {
                Debug.LogWarning($"[Spell] Không đủ điều kiện dùng {card.spellName}");
                return;
            }

            _selectedCard = card;
            _currentCaster = caster;
            _isTargeting = true;
            OnCardSelected?.Invoke(card);

            // Hiển thị vùng target hợp lệ
            ShowValidTargets(card, caster);
        }

        public void DeselectCard()
        {
            _selectedCard = null;
            _isTargeting = false;
            AreaPathManager.Instance?.HideAttackArea();
            OnCardDeselected?.Invoke();
        }

        /// <summary>
        /// Xác nhận sử dụng spell lên target. Gọi khi player click vào unit mục tiêu.
        /// </summary>
        public bool TryUseCard(UnitController target)
        {
            if (!_isTargeting || _selectedCard == null) return false;

            if (!ValidateTarget(_selectedCard, _currentCaster, target))
            {
                Debug.LogWarning($"[Spell] Target không hợp lệ cho {_selectedCard.spellName}");
                return false;
            }

            // Trừ MP
            if (!MPManager.Instance.SpendMP(_currentCaster, _selectedCard.mpCost))
            {
                Debug.LogWarning($"[Spell] Không đủ MP để dùng {_selectedCard.spellName}");
                DeselectCard();
                return false;
            }

            // Trừ action
            if (_actionLefts != null)
                _actionLefts.Value--;

            // Kích hoạt effect
            ExecuteSpell(_selectedCard, null, target, _currentCaster);

            // Xóa card nếu tiêu hao
            if (_selectedCard.consumeOnUse)
            {
                RemoveCardFromHand(_currentCaster, _selectedCard);
            }

            var usedCard = _selectedCard;
            DeselectCard();
            OnCardUsed?.Invoke(usedCard, target);

            GameMediator.Instance?.NotifySpellCardUsed(usedCard, target, _currentCaster);
            Debug.Log($"[Spell] {_currentCaster} dùng {usedCard.spellName} lên {target.name}");
            return true;
        }

        public bool IsTargeting => _isTargeting;
        public SpellCardData SelectedCard => _selectedCard;

        #region Validation

        public bool CanUseCard(SpellCardData card, PlayerID player)
        {
            if (card == null) return false;
            if (!MPManager.Instance.HasEnoughMP(player, card.mpCost)) return false;
            if (_actionLefts != null && _actionLefts.Value <= 0) return false;
            return true;
        }

        private bool ValidateTarget(SpellCardData card, PlayerID caster, UnitController target)
        {
            if (target == null || target.IsDead()) return false;

            bool isFriendly = target.GetOwner() == caster;

            return card.targetType switch
            {
                SpellTargetType.SingleAlly => isFriendly,
                SpellTargetType.SingleEnemy => !isFriendly,
                SpellTargetType.Self => isFriendly,
                SpellTargetType.AnyUnit => true,
                _ => false
            };
        }

        #endregion

        #region Effect Execution

        private void ExecuteSpell(SpellCardData card, UnitController caster, UnitController target, PlayerID casterPlayer)
        {
            var effect = SpellEffectFactory.Create(card.effectType);
            if (effect == null)
            {
                Debug.LogError($"[Spell] Không tìm thấy effect cho type {card.effectType}");
                return;
            }

            // Spawn VFX cast
            SpawnVfx(card.castVfxPrefab, caster != null ? caster.transform.position : target.transform.position);
            // Spawn VFX impact
            SpawnVfx(card.impactVfxPrefab, target.transform.position);

            effect.Apply(card,casterPlayer, target);
        }

        private void SpawnVfx(GameObject vfxPrefab, Vector3 position)
        {
            if (vfxPrefab == null) return;
            var vfx = Instantiate(vfxPrefab, position, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        #endregion

        #region Hand Management

        private void RemoveCardFromHand(PlayerID player, SpellCardData card)
        {
            if (!_playerHands.TryGetValue(player, out var hand)) return;
            hand.Remove(card);
            OnHandChanged?.Invoke(player);
        }

        #endregion

        #region Target Visualization

        private void ShowValidTargets(SpellCardData card, PlayerID caster)
        {
            var allUnits = GetAllUnitsOnMap();
            var validPositions = new List<Vector3>();
            var map = MapManager.Instance?.MapEntity;
            if (map == null) return;

            foreach (var unit in allUnits)
            {
                if (unit == null || unit.IsDead()) continue;
                if (ValidateTarget(card, caster, unit))
                {
                    validPositions.Add(map.WorldPosition(unit.currentGridPosition));
                }
            }

            if (validPositions.Count > 0)
            {
                AreaPathManager.Instance?.ShowAttackArea(validPositions);
            }
        }

        private List<UnitController> GetAllUnitsOnMap()
        {
            var result = new List<UnitController>();
            var p1Units = UnitSpawner.Instance?.GetPlayerUnits(PlayerID.Player1);
            var p2Units = UnitSpawner.Instance?.GetPlayerUnits(PlayerID.Player2);
            if (p1Units != null) result.AddRange(p1Units);
            if (p2Units != null) result.AddRange(p2Units);
            return result;
        }

        #endregion
    }
}
