using System;
using System.Collections.Generic;
using TurnBasedGame.Capture;
using TurnBasedGame.Command;
using TurnBasedGame.Core;
using TurnBasedGame.Multiplayer.Protocol;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.SpellCard
{
    public sealed class SpellRuntimeEffectManager : MonoBehaviour
    {
        private sealed class AshenMark
        {
            public ulong TargetId;
            public PlayerID TargetOwner;
            public PlayerID Caster;
            public int Damage;
            public int WeakenPercent;
            public int WeakenDuration;
            public int GuardBreakPercent;
            public int GuardBreakDuration;
            public bool Moved;

            public AshenMark Clone() => (AshenMark)MemberwiseClone();
        }

        private sealed class TemporaryBlocker
        {
            public Vector3Int Position;
            public PlayerID Owner;
            public int RemainingTurns;

            public TemporaryBlocker Clone() => (TemporaryBlocker)MemberwiseClone();
        }

        private readonly List<AshenMark> _ashenMarks = new();
        private readonly List<TemporaryBlocker> _temporaryBlockers = new();
        private bool _subscribed;

        public static SpellRuntimeEffectManager Instance { get; private set; }

        public static SpellRuntimeEffectManager InstanceOrCreate
        {
            get
            {
                if (Instance != null) return Instance;
                return new GameObject(nameof(SpellRuntimeEffectManager)).AddComponent<SpellRuntimeEffectManager>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Subscribe();
        }

        private void Start() => Subscribe();

        private void OnDestroy()
        {
            Unsubscribe();
            _temporaryBlockers.Clear();
            if (Instance == this) Instance = null;
        }

        private void Subscribe()
        {
            if (_subscribed || GameMediator.Instance == null) return;
            GameMediator.Instance.OnUnitMoved += OnUnitMoved;
            GameMediator.Instance.OnPlayerTurnEnded += OnPlayerTurnEnded;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || GameMediator.Instance == null) return;
            GameMediator.Instance.OnUnitMoved -= OnUnitMoved;
            GameMediator.Instance.OnPlayerTurnEnded -= OnPlayerTurnEnded;
            _subscribed = false;
        }

        public static bool HasLivingEnemy(PlayerID caster)
        {
            var spawner = UnitSpawner.Instance;
            if (spawner == null) return false;
            foreach (var unit in spawner.GetPlayerUnits(GetOpponent(caster)))
                if (unit != null && !unit.IsDead()) return true;
            return false;
        }

        public void ApplyAshenUltimatum(
            PlayerID caster,
            int damage,
            int weakenPercent,
            int weakenDuration,
            int guardBreakPercent,
            int guardBreakDuration,
            int maxTargets)
        {
            if (!LocalMatchAuthority.IsAuthoritative || UnitSpawner.Instance == null) return;
            Subscribe();

            var priorityBuckets = new[]
            {
                new List<UnitController>(),
                new List<UnitController>(),
                new List<UnitController>()
            };

            foreach (var unit in UnitSpawner.Instance.GetPlayerUnits(GetOpponent(caster)))
            {
                if (unit == null || unit.IsDead()) continue;
                priorityBuckets[GetAshenPriority(unit)].Add(unit);
            }

            int remainingTargets = Mathf.Clamp(maxTargets, 1, 2);
            foreach (var bucket in priorityBuckets)
            {
                while (remainingTargets > 0 && bucket.Count > 0)
                {
                    int index = LocalMatchAuthority.Random.Range(0, bucket.Count);
                    AddOrReplaceAshenMark(bucket[index], caster, damage, weakenPercent, weakenDuration,
                        guardBreakPercent, guardBreakDuration);
                    bucket.RemoveAt(index);
                    remainingTargets--;
                }

                if (remainingTargets == 0) break;
            }
        }

        private static int GetAshenPriority(UnitController unit)
        {
            bool onCapturePoint = CapturePointManager.Instance?.GetPointAt(unit.currentGridPosition) != null;
            bool hardControlled = unit.BuffHandler?.PreventsAction() == true;
            if (onCapturePoint && hardControlled) return 0;
            return onCapturePoint || hardControlled ? 1 : 2;
        }

        private static PlayerID GetOpponent(PlayerID player) =>
            player == PlayerID.Player1 ? PlayerID.Player2 : PlayerID.Player1;

        private void AddOrReplaceAshenMark(
            UnitController target,
            PlayerID caster,
            int damage,
            int weakenPercent,
            int weakenDuration,
            int guardBreakPercent,
            int guardBreakDuration)
        {
            for (int i = _ashenMarks.Count - 1; i >= 0; i--)
                if (_ashenMarks[i].TargetId == target.UnitRuntimeId) _ashenMarks.RemoveAt(i);

            _ashenMarks.Add(new AshenMark
            {
                TargetId = target.UnitRuntimeId,
                TargetOwner = target.GetOwner(),
                Caster = caster,
                Damage = Mathf.Max(0, damage),
                WeakenPercent = Mathf.Clamp(weakenPercent, 0, 100),
                WeakenDuration = Mathf.Max(1, weakenDuration),
                GuardBreakPercent = Mathf.Clamp(guardBreakPercent, 0, 100),
                GuardBreakDuration = Mathf.Max(1, guardBreakDuration)
            });
            target.BuffHandler?.AddEffect(new ActiveStatusEffect(
                StatusEffectType.AshenUltimatum, 0, 2, caster));
        }

        private void OnUnitMoved(UnitController unit, Vector3Int oldPosition, Vector3Int newPosition)
        {
            if (!LocalMatchAuthority.IsAuthoritative || unit == null || oldPosition == newPosition) return;
            foreach (var mark in _ashenMarks)
                if (mark.TargetId == unit.UnitRuntimeId) mark.Moved = true;
        }

        private void OnPlayerTurnEnded(PlayerID player)
        {
            if (!LocalMatchAuthority.IsAuthoritative) return;

            if (TurnManager.Instance == null || TurnManager.Instance.CurrentState == TurnState.GameEnd)
            {
                ClearAshenMarks();
                return;
            }

            ResolveAshenMarks(player);
            TickTemporaryBlockers();
        }

        private void ResolveAshenMarks(PlayerID player)
        {
            for (int i = _ashenMarks.Count - 1; i >= 0; i--)
            {
                var mark = _ashenMarks[i];
                if (mark.TargetOwner != player) continue;

                var target = LocalMatchAuthority.Runtime.ResolveUnit(mark.TargetId);
                bool markStillActive = target != null && !target.IsDead() && target.BuffHandler != null &&
                                       target.BuffHandler.HasEffect(StatusEffectType.AshenUltimatum);
                if (markStillActive && !mark.Moved)
                {
                    target.TakeDamage(mark.Damage);
                    if (!target.IsDead())
                    {
                        target.BuffHandler.AddEffect(new ActiveStatusEffect(
                            StatusEffectType.Weaken, mark.WeakenPercent, mark.WeakenDuration, mark.Caster));
                        target.BuffHandler.AddEffect(new ActiveStatusEffect(
                            StatusEffectType.GuardBreak, mark.GuardBreakPercent, mark.GuardBreakDuration, mark.Caster));
                    }
                }

                RemoveAshenStatus(target);
                _ashenMarks.RemoveAt(i);
            }
        }

        private void ClearAshenMarks()
        {
            foreach (var mark in _ashenMarks)
            {
                var target = LocalMatchAuthority.Runtime.ResolveUnit(mark.TargetId);
                RemoveAshenStatus(target);
            }
            _ashenMarks.Clear();
        }

        private static void RemoveAshenStatus(UnitController target)
        {
            if (target != null && !target.IsDead())
                target.BuffHandler?.RemoveByType(StatusEffectType.AshenUltimatum);
        }

        public static bool CanPlaceTemporaryBlocker(Vector3Int position)
        {
            var map = MapManager.Instance;
            var tile = map?.MapEntity?.Tile(position);
            if (tile == null || !tile.Vacant || map.HasUnitAtTile(position)) return false;
            if (Instance != null && Instance.HasTemporaryBlocker(position)) return false;
            if (CapturePointManager.Instance?.GetPointAt(position) != null) return false;
            if (UnitSpawner.Instance != null)
                foreach (var point in UnitSpawner.Instance.SpawnPoints)
                    if (point != null && point.GridPosition == position) return false;
            return true;
        }

        public bool TryAddTemporaryBlocker(Vector3Int position, int duration, PlayerID owner)
        {
            if (!LocalMatchAuthority.IsAuthoritative || !CanPlaceTemporaryBlocker(position)) return false;
            Subscribe();

            _temporaryBlockers.Add(new TemporaryBlocker
            {
                Position = position,
                Owner = owner,
                RemainingTurns = Mathf.Max(1, duration)
            });
            return true;
        }

        private void TickTemporaryBlockers()
        {
            for (int i = _temporaryBlockers.Count - 1; i >= 0; i--)
            {
                _temporaryBlockers[i].RemainingTurns--;
                if (_temporaryBlockers[i].RemainingTurns > 0) continue;
                _temporaryBlockers.RemoveAt(i);
            }
        }

        public bool HasTemporaryBlocker(Vector3Int position)
        {
            foreach (var blocker in _temporaryBlockers)
                if (blocker.Position == position) return true;
            return false;
        }

        public bool HasTemporaryBlockers => _temporaryBlockers.Count > 0;

        internal Action CaptureRollback()
        {
            var marks = new List<AshenMark>(_ashenMarks.Count);
            foreach (var mark in _ashenMarks) marks.Add(mark.Clone());
            var blockers = new List<TemporaryBlocker>(_temporaryBlockers.Count);
            foreach (var blocker in _temporaryBlockers) blockers.Add(blocker.Clone());

            return () =>
            {
                _ashenMarks.Clear();
                _temporaryBlockers.Clear();
                foreach (var mark in marks) _ashenMarks.Add(mark.Clone());
                foreach (var blocker in blockers)
                    _temporaryBlockers.Add(blocker.Clone());
            };
        }

        internal void AppendState(List<MatchStateChange> changes)
        {
            foreach (var mark in _ashenMarks)
            {
                var target = LocalMatchAuthority.Runtime.ResolveUnit(mark.TargetId);
                if (target == null || target.IsDead()) continue;
                changes.Add(new MatchStateChange
                {
                    Kind = StateChangeKind.AshenMark,
                    Player = (PlayerId)mark.Caster,
                    Entity = mark.TargetId,
                    Position = new GridCoordinate(mark.WeakenDuration, mark.GuardBreakDuration, mark.Moved ? 1 : 0),
                    Value = mark.Damage,
                    Value2 = mark.WeakenPercent,
                    Value3 = mark.GuardBreakPercent,
                    Value4 = (int)mark.TargetOwner
                });
            }

            foreach (var blocker in _temporaryBlockers)
                changes.Add(new MatchStateChange
                {
                    Kind = StateChangeKind.TemporaryBlocker,
                    Player = (PlayerId)blocker.Owner,
                    Position = new GridCoordinate(blocker.Position.x, blocker.Position.y, blocker.Position.z),
                    Value = blocker.RemainingTurns
                });
        }

        internal void ApplyReplica(IEnumerable<MatchStateChange> state)
        {
            _temporaryBlockers.Clear();
            _ashenMarks.Clear();
            foreach (var entry in state)
            {
                if (entry.Kind == StateChangeKind.AshenMark)
                {
                    _ashenMarks.Add(new AshenMark
                    {
                        TargetId = entry.Entity,
                        TargetOwner = (PlayerID)entry.Value4,
                        Caster = (PlayerID)entry.Player,
                        Damage = entry.Value,
                        WeakenPercent = entry.Value2,
                        WeakenDuration = entry.Position.X,
                        GuardBreakPercent = entry.Value3,
                        GuardBreakDuration = entry.Position.Y,
                        Moved = entry.Position.Z != 0
                    });
                }
                else if (entry.Kind == StateChangeKind.TemporaryBlocker)
                {
                    var position = new Vector3Int(entry.Position.X, entry.Position.Y, entry.Position.Z);
                    _temporaryBlockers.Add(new TemporaryBlocker
                    {
                        Position = position,
                        Owner = (PlayerID)entry.Player,
                        RemainingTurns = entry.Value
                    });
                }
            }
        }
    }
}
