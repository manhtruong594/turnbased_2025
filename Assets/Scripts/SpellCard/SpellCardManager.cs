using System;
using System.Collections.Generic;
using RedBjorn.ProtoTiles;
using RedBjorn.ProtoTiles.Example;
using TurnBasedGame.Core;
using TurnBasedGame.ObjectPool;
using TurnBasedGame.Resources;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.SpellCard
{
    /// <summary>
    /// Runtime manager xử lý toàn bộ logic sử dụng Spell Card trong trận đấu.
    /// Sử dụng State Pattern để quản lý flow (Idle → Targeting → Confirming).
    /// Sử dụng Command Pattern để encapsulate spell execution.
    /// Sử dụng Object Pool cho VFX spawning.
    /// </summary>
    public class SpellCardManager : BaseManager
    {
        public static SpellCardManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private IntReference _actionLefts;

        [Header("Confirm UI")]
        [SerializeField] private SpellCardConfirmUI _confirmUI;

        private readonly Dictionary<PlayerID, List<SpellCardData>> _playerHands = new();
        private SpellCardState _currentState;

        // State context — exposed cho State & Command classes
        public SpellCardData SelectedCard { get; set; }
        public PlayerID CurrentCaster { get; set; }
        public TileEntity CachedTile { get; set; }
        public MapEntity CachedMap { get; private set; }
        public SpellCardConfirmUI ConfirmUI => _confirmUI;

        // Events cho UI binding
        public event Action<SpellCardData> OnCardSelected;
        public event Action OnCardDeselected;

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
                _confirmUI.OnConfirmed += HandleConfirm;
                _confirmUI.OnCanceled += HandleCancel;
            }

            SetState(new SpellIdleState());
        }

        private void OnDestroy()
        {
            if (_confirmUI != null)
            {
                _confirmUI.OnConfirmed -= HandleConfirm;
                _confirmUI.OnCanceled -= HandleCancel;
            }
        }

        void Update() => _currentState?.Update(this);

        private void HandleConfirm() => _currentState?.OnConfirm(this);
        private void HandleCancel() => _currentState?.OnCancel(this);

        #region State Management

        public void SetState(SpellCardState newState)
        {
            _currentState?.Exit(this);
            _currentState = newState;
            _currentState?.Enter(this);
        }

        #endregion

        #region Hand Management

        /// <summary>
        /// Khởi tạo hand cho player từ danh sách spell đã chọn trước trận.
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

        public void RemoveCardFromHand(PlayerID player, SpellCardData card)
        {
            if (!_playerHands.TryGetValue(player, out var hand)) return;
            hand.Remove(card);
            GameMediator.Instance?.NotifyHandChanged(player);
        }

        #endregion

        #region Card Selection

        /// <summary>
        /// Player chọn 1 spell card từ UI => chuyển sang trạng thái chọn target.
        /// </summary>
        public void SelectCard(SpellCardData card, PlayerID caster, RectTransform sourceCardRect = null)
        {
            if (card == null) return;
            if (_currentState is SpellConfirmingState) return;
            if (!CanUseCard(card, caster))
            {
                Debug.LogWarning($"[Spell] Không đủ điều kiện dùng {card.spellName}");
                return;
            }

            SelectedCard = card;
            CurrentCaster = caster;
            SetState(new SpellTargetingState());
        }

        public void DeselectCard()
        {
            SetState(new SpellIdleState());
        }

        #endregion

        #region Validation

        public bool CanUseCard(SpellCardData card, PlayerID player)
        {
            if (card == null) return false;
            if (!MPManager.Instance.HasEnoughMP(player, card.mpCost)) return false;
            return true;
        }

        public bool ValidateTarget(SpellCardData card, PlayerID caster, UnitController target)
        {
            if (target == null || target.IsDead()) return false;

            bool isFriendly = target.GetOwner() == caster;

            return card.targetType switch
            {
                SpellTargetType.SingleAlly => isFriendly,
                SpellTargetType.SingleEnemy => !isFriendly,
                SpellTargetType.Self => isFriendly,
                SpellTargetType.AllAllies => isFriendly,
                SpellTargetType.AllEnemies => !isFriendly,
                SpellTargetType.AnyUnit => true,
                _ => false
            };
        }

        #endregion

        #region Target Visualization

        public void ShowSpellRange()
        {
            if (SelectedCard == null || CachedMap == null) return;

            var mousePos = MyInput.GroundPosition(CachedMap.Settings.Plane());
            var tile = CachedMap.Tile(mousePos);
            if (tile == null)
            {
                AreaPathManager.Instance?.HideSpellArea();
                return;
            }

            var border = CachedMap.WalkableBorder(tile.Position, SelectedCard.range > 0 ? SelectedCard.range : 100);
            AreaPathManager.Instance?.ShowSpellArea(border);
        }

        public void ShowConfirmUI()
        {
            _confirmUI?.Show(SelectedCard, new RectTransform());
        }

        #endregion

        #region VFX (Object Pool)

        public void SpawnSpellVfx(SpellCardData card, UnitController target)
        {
            SpawnVfx(card.castVfxPrefab, target.transform.position);
            SpawnVfx(card.impactVfxPrefab, target.transform.position);
        }

        private void SpawnVfx(GameObject vfxPrefab, Vector3 position)
        {
            if (vfxPrefab == null) return;

            var pool = ObjectPoolManager.Instance;
            if (pool != null)
            {
                var vfx = pool.Spawn(vfxPrefab, position, Quaternion.identity);
                if (vfx != null)
                {
                    var pooledObj = vfx.GetComponent<PooledObject>();
                    pooledObj?.DespawnAfter(3f);
                    return;
                }
            }

            // Fallback nếu pool chưa sẵn sàng
            var fallback = Instantiate(vfxPrefab, position, Quaternion.identity);
            Destroy(fallback, 3f);
        }

        #endregion

        #region Utility

        public List<UnitController> GetAllUnitsOnMap()
        {
            var result = new List<UnitController>();
            var p1Units = UnitSpawner.Instance?.GetPlayerUnits(PlayerID.Player1);
            var p2Units = UnitSpawner.Instance?.GetPlayerUnits(PlayerID.Player2);
            if (p1Units != null) result.AddRange(p1Units);
            if (p2Units != null) result.AddRange(p2Units);
            return result;
        }

        public void NotifyCardSelected() => OnCardSelected?.Invoke(SelectedCard);
        public void NotifyCardDeselected() => OnCardDeselected?.Invoke();

        #endregion

        public override void SetCachedMap(MapEntity map) => CachedMap = map;
    }
}
