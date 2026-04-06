using System;
using System.Collections.Generic;
using RedBjorn.ProtoTiles;
using RedBjorn.ProtoTiles.Example;
using TurnBasedGame.Core;
using TurnBasedGame.Resources;
using TurnBasedGame.Unit;
using UnityEngine;
using UnityEngine.EventSystems;

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

        [Header("Confirm UI")]
        [SerializeField] private SpellCardConfirmUI _confirmUI;

        private readonly Dictionary<PlayerID, List<SpellCardData>> _playerHands = new();
        private SpellCardData _selectedCard;
        private PlayerID _currentCaster;
        private bool _isTargeting;
        private bool _isConfirming;

        // Events cho UI binding
        public event Action<SpellCardData> OnCardSelected;
        public event Action OnCardDeselected;
        MapEntity _cachedMap;
        TileEntity _cachedTile;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_confirmUI != null)
            {
                _confirmUI.OnConfirmed += OnConfirmCard;
                _confirmUI.OnCanceled += OnCancelCard;
            }
        }

        private void OnDestroy()
        {
            if (_confirmUI != null)
            {
                _confirmUI.OnConfirmed -= OnConfirmCard;
                _confirmUI.OnCanceled -= OnCancelCard;
            }
        }

        void Update()
        {
            if (!_isTargeting || _isConfirming) return;
            var mousePos = MyInput.GroundPosition(_cachedMap.Settings.Plane());
            if (MyInput.GetOnWorldUp(_cachedMap.Settings.Plane()) && !EventSystem.current.IsPointerOverGameObject())
            {
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    DeselectCard();
                    return;
                }
                var tileClicked = _cachedMap.Tile(mousePos);
                if (tileClicked == null)
                    return;

                ShowConfirmUI(tileClicked);
            }
            ShowSpellRange(_selectedCard, _currentCaster);
        }

        private void ShowConfirmUI(TileEntity tileClicked)
        {
            if (_confirmUI == null) return;
            _isConfirming = true;
            _cachedTile = tileClicked;
            _confirmUI.Show(_selectedCard, new RectTransform()); // todo: truyền rect của card gốc để có animation bay từ đó
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
            GameMediator.Instance?.NotifyHandChanged(player);
        }

        public IReadOnlyList<SpellCardData> GetHand(PlayerID player)
        {
            return _playerHands.TryGetValue(player, out var hand) ? hand : Array.Empty<SpellCardData>();
        }

        /// <summary>
        /// Player chọn 1 spell card từ UI => bắt đầu chế độ chọn target.
        /// </summary>
        public void SelectCard(SpellCardData card, PlayerID caster, RectTransform sourceCardRect = null)
        {
            if (card == null) return;
            if (_isConfirming) return;
            if (!CanUseCard(card, caster))
            {
                Debug.LogWarning($"[Spell] Không đủ điều kiện dùng {card.spellName}");
                return;
            }
            if (_isTargeting)
            {
                DeselectCard();
            }
            _selectedCard = card;
            _currentCaster = caster;
            OnCardSelected?.Invoke(card);
            _isTargeting = true;
        }

        public void DeselectCard()
        {
            _selectedCard = null;
            _isTargeting = false;
            _isConfirming = false;
            _confirmUI?.Hide();
            AreaPathManager.Instance?.HideSpellArea();
            OnCardDeselected?.Invoke();
        }

        private void OnConfirmCard()
        {
            if (TryUseCard(_cachedTile))
            {
                GameMediator.Instance?.NotifySpellCardUsed(_selectedCard, _currentCaster);
                Debug.Log($"[Spell] {_currentCaster} dùng {_selectedCard.spellName}");
                _isConfirming = false;
                _isTargeting = false;
                DeselectCard();
            }
            else
            {
                Debug.Log("Cannot use spell on this tile.");
                OnCancelCard();
            }
        }

        private void OnCancelCard()
        {
            _isConfirming = false;
            _isTargeting = false;
            DeselectCard();
        }

        /// <summary>
        /// Xác nhận sử dụng spell lên target. Gọi khi player click vào unit mục tiêu.
        /// Với AllAllies/AllEnemies, target là một unit đại diện — spell tự apply lên tất cả.
        /// </summary>
        public bool TryUseCard(TileEntity target)
        {
            if (!_isTargeting || _selectedCard == null) return false;

            // Trừ MP
            if (!MPManager.Instance.SpendMP(_currentCaster, _selectedCard.mpCost))
            {
                Debug.LogWarning($"[Spell] Không đủ MP để dùng {_selectedCard.spellName}");
                return false;
            }

            if (_selectedCard.range == 0)
            {
                // range = 0 nghĩa là ảnh hưởng toàn bản đồ, không cần check khoảng cách
                var allUnits = GetAllUnitsOnMap();
                foreach (var unit in allUnits)
                {
                    if (ValidateTarget(_selectedCard, _currentCaster, unit))
                        ExecuteSpell(_selectedCard, null, unit, _currentCaster);
                }
            }
            else if (IsMultiTarget(_selectedCard.targetType))
            {
                var unitsInRange = MapManager.Instance.GetUnitsInRange(target, _selectedCard.range);
                foreach (var unit in unitsInRange)
                {
                    if (ValidateTarget(_selectedCard, _currentCaster, unit))
                    {
                        ExecuteSpell(_selectedCard, null, unit, _currentCaster);
                    }
                }
            }
            else
            {
                ExecuteSpell(_selectedCard, null, MapManager.Instance.GetUnitAtTile(target.Position), _currentCaster);
            }

            // Xóa card nếu tiêu hao
            if (_selectedCard.consumeOnUse)
            {
                RemoveCardFromHand(_currentCaster, _selectedCard);
            }
            return true;
        }

        #region Validation

        public bool CanUseCard(SpellCardData card, PlayerID player)
        {
            if (card == null) return false;
            if (!MPManager.Instance.HasEnoughMP(player, card.mpCost)) return false;
            //if (_actionLefts != null && _actionLefts.Value <= 0) return false;
            return true;
        }

        private bool ValidateTarget(SpellCardData card, PlayerID caster, UnitController target)
        {
            if (target == null || target.IsDead()) return false;

            bool isFriendly = target.GetOwner() == caster;

            bool ownerValid = card.targetType switch
            {
                SpellTargetType.SingleAlly => isFriendly,
                SpellTargetType.SingleEnemy => !isFriendly,
                SpellTargetType.Self => isFriendly,
                SpellTargetType.AllAllies => isFriendly,
                SpellTargetType.AllEnemies => !isFriendly,
                SpellTargetType.AnyUnit => true,
                _ => false
            };

            if (!ownerValid) return false;
            return true;
        }

        private bool IsMultiTarget(SpellTargetType type)
        {
            return type == SpellTargetType.AllAllies || type == SpellTargetType.AllEnemies || type == SpellTargetType.AnyUnit;
        }

        #endregion

        #region Effect Execution

        private void ExecuteSpell(SpellCardData card, UnitController caster, UnitController target, PlayerID casterPlayer)
        {
            if (card.spellEffect == null)
            {
                Debug.LogError($"[Spell] Chưa gán effect cho {card.spellName}");
                return;
            }

            SpawnVfx(card.castVfxPrefab, caster != null ? caster.transform.position : target.transform.position);
            SpawnVfx(card.impactVfxPrefab, target.transform.position);

            card.Cast(casterPlayer, target);
        }

        private void SpawnVfx(GameObject vfxPrefab, Vector3 position)
        {
            // todo: spawn VFX với pooling để tối ưu hiệu năng
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
            GameMediator.Instance?.NotifyHandChanged(player);
        }

        #endregion

        #region Target Visualization

        private void ShowSpellRange(SpellCardData card, PlayerID caster)
        {
            if (card == null) return;

            var mousePos = MyInput.GroundPosition(_cachedMap.Settings.Plane());
            var tile = _cachedMap.Tile(mousePos);
            if (tile == null)
            {
                AreaPathManager.Instance?.HideSpellArea();
                return;
            }

            var border = _cachedMap.WalkableBorder(tile.Position, card.range > 0 ? card.range : 100);
            AreaPathManager.Instance?.ShowSpellArea(border);
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

        public override void SetCachedMap(MapEntity map)
        {
            _cachedMap = map;
        }
    }
}
