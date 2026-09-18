using System;
using System.Collections.Generic;
using RedBjorn.ProtoTiles;
using RedBjorn.ProtoTiles.Example;
using TurnBasedGame.Core;
using TurnBasedGame.ObjectPool;
using TurnBasedGame.Resources;
using TurnBasedGame.Unit;
using UnityEngine;
using TurnBasedGame.Command;
using TurnBasedGame.Multiplayer.Protocol;

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

        private readonly Dictionary<PlayerID, List<SpellCardData>> _playerHands = new();
        private readonly Dictionary<PlayerID, List<ulong>> _handIds = new();
        private ulong _nextCardId;
        public ulong SelectedCardInstanceId { get; private set; }
        internal void ApplyReplicaHand(MatchStateChange[] state)
        {
            _playerHands.Clear(); _handIds.Clear();
            foreach (var entry in state)
            {
                if (entry.Kind != StateChangeKind.HandCard) continue;
                var player = (PlayerID)entry.Player;
                if (!_playerHands.ContainsKey(player))
                {
                    _playerHands[player] = new List<SpellCardData>();
                    _handIds[player] = new List<ulong>();
                }
                _playerHands[player].Add(LocalMatchAuthority.Content.Resolve<SpellCardData>(entry.ContentId));
                _handIds[player].Add(entry.Entity);
            }
        }

        internal Action CaptureRollback()
        {
            var hands = new Dictionary<PlayerID, List<SpellCardData>>();
            var ids = new Dictionary<PlayerID, List<ulong>>();
            foreach (var pair in _playerHands) hands[pair.Key] = new List<SpellCardData>(pair.Value);
            foreach (var pair in _handIds) ids[pair.Key] = new List<ulong>(pair.Value);
            ulong counter = _nextCardId;
            return () =>
            {
                _playerHands.Clear(); _handIds.Clear(); _nextCardId = counter;
                foreach (var pair in hands) _playerHands.Add(pair.Key, pair.Value);
                foreach (var pair in ids) _handIds.Add(pair.Key, pair.Value);
            };
        }
        private SpellCardState _currentState;

        // State context — exposed cho State & Command classes
        public SpellCardData SelectedCard { get; set; }
        public PlayerID CurrentCaster { get; set; }
        public TileEntity CachedTile { get; set; }
        public MapEntity CachedMap { get; private set; }
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

            SetState(new SpellIdleState());
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
            if (!LocalMatchAuthority.IsAuthoritative) return;
            var hand = new List<SpellCardData>();
            var ids = new List<ulong>();
            if (spells != null)
            {
                foreach (var spell in spells)
                {
                    if (spell != null) { hand.Add(spell); ids.Add(++_nextCardId); }
                }
            }
            _playerHands[player] = hand;
            _handIds[player] = ids;
            GameMediator.Instance?.NotifyHandChanged(player);
        }

        public IReadOnlyList<SpellCardData> GetHand(PlayerID player)
        {
            return _playerHands.TryGetValue(player, out var hand) ? hand : Array.Empty<SpellCardData>();
        }

        public void RemoveCardFromHand(PlayerID player, SpellCardData card)
        {
            if (!LocalMatchAuthority.IsAuthoritative) return;
            if (!_playerHands.TryGetValue(player, out var hand)) return;
            int index = hand.IndexOf(card);
            if (index < 0) return;
            hand.RemoveAt(index);
            _handIds[player].RemoveAt(index);
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
            SelectedCardInstanceId = GetCardInstanceId(caster, card);
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
            if (!TurnBasedGame.Multiplayer.MatchGameplayBootstrap.CanControl(player)) return false;
            if (card == null) return false;
            if (TurnManager.Instance == null || TurnManager.Instance.CurrentPlayer != player ||
                TurnManager.Instance.IsTurnTransitionPending || TurnManager.Instance.CurrentState == TurnState.GameEnd) return false;
            if (GetCardInstanceId(player, card) == 0 || MPManager.Instance == null ||
                !MPManager.Instance.HasEnoughMP(player, card.mpCost)) return false;
            return true;
        }

        public ulong GetCardInstanceId(PlayerID player, SpellCardData card)
        {
            if (!_playerHands.TryGetValue(player, out var hand)) return 0;
            int index = hand.IndexOf(card);
            return index < 0 ? 0 : _handIds[player][index];
        }

        internal void AppendHandState(List<MatchStateChange> changes)
        {
            foreach (var pair in _playerHands)
                for (int i = 0; i < pair.Value.Count; i++)
                    changes.Add(new MatchStateChange { Kind = StateChangeKind.HandCard, Player = (PlayerId)pair.Key,
                        Entity = _handIds[pair.Key][i], ContentId = LocalMatchAuthority.Content.GetId(pair.Value[i]) });
        }

        internal (CommandReason, string) CastAuthorized(PlayerID actor, ulong cardId, Vector3Int targetPosition)
        {
            if (!_handIds.TryGetValue(actor, out var ids)) return (CommandReason.InvalidCard, "Hand không tồn tại.");
            int index = ids.IndexOf(cardId);
            if (index < 0) return (CommandReason.InvalidCard, "Card instance không còn trong hand của actor.");
            var card = _playerHands[actor][index];
            if (card == null || card.spellEffect == null || card.mpCost < 0)
                return (CommandReason.InvalidCard, "Card không có effect hợp lệ.");
            var tile = CachedMap?.Tile(targetPosition);
            if (tile == null) return (CommandReason.InvalidTarget, "Target tile không tồn tại.");
            bool area = card.targetType == SpellTargetType.AllAllies || card.targetType == SpellTargetType.AllEnemies;
            var candidates = area
                ? (card.range == 0 ? GetAllUnitsOnMap() : MapManager.Instance.GetUnitsInRange(tile, card.range))
                : new List<UnitController> { MapManager.Instance.GetUnitAtTile(targetPosition) };
            var targets = new List<UnitController>();
            foreach (var unit in candidates)
                if (ValidateTarget(card, actor, unit)) targets.Add(unit);
            if (targets.Count == 0) return (CommandReason.InvalidTarget, "Không có target hợp lệ.");
            if (MPManager.Instance == null || !MPManager.Instance.HasEnoughMP(actor, card.mpCost))
                return (CommandReason.InsufficientMP, "Không đủ MP để cast spell.");

            // All rejection paths precede the transaction. Never trust a client-supplied target list/cost.
            if (!MPManager.Instance.SpendMP(actor, card.mpCost)) return (CommandReason.InsufficientMP, "Không đủ MP.");
            if (card.consumeOnUse)
            {
                _playerHands[actor].RemoveAt(index);
                ids.RemoveAt(index);
            }
            foreach (var unit in targets) card.Cast(actor, unit);
            LocalMatchAuthority.PublishAfterCommit(() =>
            {
                if (card.consumeOnUse) GameMediator.Instance?.NotifyHandChanged(actor);
                GameMediator.Instance?.NotifySpellCardUsed(card, actor);
                foreach (var unit in targets)
                    if (unit != null) SpawnSpellVfx(card, unit);
            });
            return (CommandReason.None, null);
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
            TurnBasedGame.UI.BattleHUDToolkit.Instance?.ShowSpellConfirmation(
                SelectedCard,
                HandleConfirm,
                HandleCancel);
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
