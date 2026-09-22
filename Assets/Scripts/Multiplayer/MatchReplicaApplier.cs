using System;
using System.Collections.Generic;
using TurnBasedGame.Command;
using TurnBasedGame.Core;
using TurnBasedGame.Capture;
using TurnBasedGame.Resources;
using TurnBasedGame.Skills;
using TurnBasedGame.SpellCard;
using TurnBasedGame.Unit;
using TurnBasedGame.Multiplayer.Protocol;
using UnityEngine;

namespace TurnBasedGame.Multiplayer
{
    public sealed class MatchReplicaApplier
    {
        private static Vector3Int Grid(MatchStateChange s) => new Vector3Int(s.Position.X, s.Position.Y, s.Position.Z);

        public void Apply(MatchSnapshot snapshot, PlayerId viewer)
        {
            if (LocalMatchAuthority.IsAuthoritative) throw new InvalidOperationException("Cannot apply replica on authority.");
            var state = snapshot.State.StateChanges;
            Validate(state, viewer);
            var ids = new HashSet<ulong>();
            foreach (var entry in state) if (entry.Kind == StateChangeKind.Unit) ids.Add(entry.Entity);
            for (int p = 1; p <= 2; p++)
                foreach (var unit in UnitSpawner.Instance.GetPlayerUnits((PlayerID)p).ToArray())
                {
                    MapManager.Instance.UnregisterUnit(unit.currentGridPosition);
                    if (!ids.Contains(unit.UnitRuntimeId)) UnitSpawner.Instance.RemoveReplica(unit);
                }
            foreach (var entry in state)
            {
                if (entry.Kind != StateChangeKind.Unit) continue;
                var unit = LocalMatchAuthority.Runtime.ResolveUnit(entry.Entity) ?? UnitSpawner.Instance.CreateReplica(entry);
                unit.ApplyReplica(entry);
                foreach (var skill in unit.AttackComponent.ActiveSkills)
                    if (skill is SkillBase active) active.ApplyReplicaCooldown(0);
                var effects = new List<ActiveStatusEffect>();
                foreach (var child in state)
                {
                    if (child.Entity != entry.Entity) continue;
                    if (child.Kind == StateChangeKind.Cooldown)
                        foreach (var skill in unit.AttackComponent.ActiveSkills)
                            if (skill is SkillBase active && active.SkillContentId == child.ContentId)
                                active.ApplyReplicaCooldown(child.Value);
                    if (child.Kind == StateChangeKind.Status)
                        effects.Add(new ActiveStatusEffect((StatusEffectType)child.Value, child.Value2,
                            child.Value4, (PlayerID)child.Player).WithRemainingTurns(child.Value3));
                }
                unit.BuffHandler?.ApplyReplicaEffects(effects);
                unit.AttackComponent.RefreshReplica();
            }
            var hazards = new List<TileHazardInstance>();
            foreach (var entry in state)
            {
                switch (entry.Kind)
                {
                    case StateChangeKind.MP: MPManager.Instance.ApplyReplica((PlayerID)entry.Player, entry.Value); break;
                    case StateChangeKind.Turn: TurnManager.Instance.ApplyReplica(entry, snapshot.Deadline); break;
                    case StateChangeKind.Dice: LocalMatchAuthority.ApplyReplicaDice(entry); break;
                    case StateChangeKind.SpawnPoint:
                        var point = LocalMatchAuthority.Runtime.ResolveSpawnPoint(entry.ContentId);
                        if (entry.Value == 1) point.MarkAsAvailable(); else point.MarkAsOccupied();
                        break;
                    case StateChangeKind.Capture:
                        foreach (var capture in CapturePointManager.Instance.CapturePoints)
                            if (capture.GridPosition == Grid(entry)) capture.RestoreOwnerAuthorized(entry.Player == 0 ? null : (PlayerID?)entry.Player);
                        break;
                    case StateChangeKind.Hazard:
                        hazards.Add(new TileHazardInstance((TileHazardType)entry.Value, Grid(entry), entry.Value3,
                            (PlayerID)entry.Player, entry.Value2, entry.Scalar, entry.Value4)); break;
                }
            }
            if (hazards.Count > 0 || TileHazardManager.Instance != null) TileHazardManager.InstanceOrCreate.ApplyReplica(hazards);
            bool hasRuntimeSpellState = false;
            foreach (var entry in state)
                if (entry.Kind == StateChangeKind.AshenMark || entry.Kind == StateChangeKind.TemporaryBlocker)
                    hasRuntimeSpellState = true;
            if (hasRuntimeSpellState || SpellRuntimeEffectManager.Instance != null)
                SpellRuntimeEffectManager.InstanceOrCreate.ApplyReplica(state);
            SpellCardManager.Instance.ApplyReplicaHand(state);
            var actual = MatchSnapshotProtocol.Visible(LocalMatchAuthority.CaptureState().ToArray(), viewer);
            if (MatchSnapshotProtocol.Hash(actual, TurnManager.Instance.Deadline) != snapshot.Hash)
                throw new InvalidOperationException("Replica state hash mismatch after apply.");
        }

        private static void Validate(MatchStateChange[] state, PlayerId viewer)
        {
            if (GameMediator.Instance == null || !GameMediator.Instance.IsInitialized || UnitSpawner.Instance == null ||
                SpellCardManager.Instance == null || MPManager.Instance == null || TurnManager.Instance == null)
                throw new InvalidOperationException("Gameplay managers not ready.");
            var units = new Dictionary<ulong, MatchStateChange>();
            var positions = new HashSet<Vector3Int>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            int mp = 0, turn = 0, dice = 0, spawns = 0, captures = 0;
            foreach (var entry in state)
            {
                if (entry.Kind == StateChangeKind.Unit)
                {
                    var prefab = LocalMatchAuthority.Content.ResolveUnitPrefab(entry.ContentId);
                    if (entry.Entity == 0 || prefab == null || !MatchProtocol.IsPlayer(entry.Player) || entry.Value <= 0 ||
                        entry.Value2 < 0 || entry.Value2 > 1 || entry.Value3 < 0 || entry.Value3 > 1 ||
                        !units.TryAdd(entry.Entity, entry) || !positions.Add(Grid(entry)) ||
                        MapManager.Instance.MapEntity.Tile(Grid(entry)) == null) throw new ArgumentException("Invalid unit state.");
                    var existing = LocalMatchAuthority.Runtime.ResolveUnit(entry.Entity);
                    if (existing != null && (existing.UnitContentId != entry.ContentId || (PlayerId)existing.GetOwner() != entry.Player))
                        throw new ArgumentException("Unit identity changed.");
                }
            }
            foreach (var entry in state)
            {
                string key = null;
                switch (entry.Kind)
                {
                    case StateChangeKind.Unit: break;
                    case StateChangeKind.MP:
                        if (!MatchProtocol.IsPlayer(entry.Player) || entry.Value < 0 || entry.Value > MPManager.Instance.MaxMP)
                            throw new ArgumentException("Invalid MP state.");
                        key = "mp:" + (int)entry.Player; mp++; break;
                    case StateChangeKind.Turn:
                        if (!MatchProtocol.IsPlayer(entry.Player) || entry.Value < 0 || !Enum.IsDefined(typeof(TurnState), entry.Value2) ||
                            entry.Value3 < 0 || entry.Value3 > 2) throw new ArgumentException("Invalid turn state.");
                        turn++; break;
                    case StateChangeKind.Dice:
                        if (entry.Value2 < 0 || (entry.Value2 & ~6) != 0) throw new ArgumentException("Invalid dice state.");
                        dice++; break;
                    case StateChangeKind.HandCard:
                        if (entry.Player != viewer || entry.Entity == 0 || LocalMatchAuthority.Content.Resolve<SpellCardData>(entry.ContentId) == null)
                            throw new ArgumentException("Invalid private hand.");
                        key = "card:" + entry.Entity; break;
                    case StateChangeKind.Cooldown:
                        if (!units.TryGetValue(entry.Entity, out var owner) || owner.Player != entry.Player || entry.Value < 0)
                            throw new ArgumentException("Invalid cooldown owner.");
                        bool found = false;
                        foreach (var skill in LocalMatchAuthority.Content.ResolveUnitPrefab(owner.ContentId).UnitData.StartingSkills)
                            if (LocalMatchAuthority.Content.GetId(skill) == entry.ContentId) found = true;
                        if (!found) throw new ArgumentException("Invalid cooldown skill.");
                        key = "skill:" + entry.Entity + ":" + entry.ContentId; break;
                    case StateChangeKind.Status:
                        if (!units.ContainsKey(entry.Entity) || !MatchProtocol.IsPlayer(entry.Player) ||
                            !Enum.IsDefined(typeof(StatusEffectType), entry.Value) || entry.Value4 < 1 || entry.Value3 > entry.Value4)
                            throw new ArgumentException("Invalid status state.");
                        key = "status:" + entry.Entity + ":" + entry.Value; break;
                    case StateChangeKind.SpawnPoint:
                        var spawn = LocalMatchAuthority.Runtime.ResolveSpawnPoint(entry.ContentId);
                        if (spawn == null || (PlayerId)spawn.Owner != entry.Player || entry.Value < 0 || entry.Value > 1)
                            throw new ArgumentException("Invalid spawn point.");
                        key = "spawn:" + entry.ContentId; spawns++; break;
                    case StateChangeKind.Capture:
                        bool matched = false;
                        if (CapturePointManager.Instance != null)
                            foreach (var capture in CapturePointManager.Instance.CapturePoints)
                                if (capture.GridPosition == Grid(entry)) matched = true;
                        if (!matched || (entry.Player != 0 && !MatchProtocol.IsPlayer(entry.Player))) throw new ArgumentException("Invalid capture point.");
                        key = "capture:" + Grid(entry); captures++; break;
                    case StateChangeKind.Hazard:
                        if (!Enum.IsDefined(typeof(TileHazardType), entry.Value) || !MatchProtocol.IsPlayer(entry.Player) ||
                            MapManager.Instance.MapEntity.Tile(Grid(entry)) == null || entry.Value3 < 1 || entry.Value4 < 1 ||
                            float.IsNaN(entry.Scalar) || entry.Scalar < 0 || entry.Scalar > 1) throw new ArgumentException("Invalid hazard.");
                        key = "hazard:" + Grid(entry) + ":" + entry.Value; break;
                    case StateChangeKind.AshenMark:
                        if (!units.TryGetValue(entry.Entity, out var markedUnit) || !MatchProtocol.IsPlayer(entry.Player) ||
                            !MatchProtocol.IsPlayer((PlayerId)entry.Value4) || markedUnit.Player != (PlayerId)entry.Value4 ||
                            entry.Value < 0 || entry.Value2 < 0 || entry.Value2 > 100 ||
                            entry.Value3 < 0 || entry.Value3 > 100 || entry.Position.X < 1 || entry.Position.Y < 1 ||
                            entry.Position.Z < 0 || entry.Position.Z > 1)
                            throw new ArgumentException("Invalid Ashen Ultimatum mark.");
                        key = "ashen:" + entry.Entity; break;
                    case StateChangeKind.TemporaryBlocker:
                        if (!MatchProtocol.IsPlayer(entry.Player) || MapManager.Instance.MapEntity.Tile(Grid(entry)) == null ||
                            entry.Value < 1 || positions.Contains(Grid(entry)))
                            throw new ArgumentException("Invalid temporary blocker.");
                        key = "blocker:" + Grid(entry); break;
                    default: throw new ArgumentException("Unexpected/private state entry.");
                }
                if (key != null && !keys.Add(key)) throw new ArgumentException("Duplicate state entry.");
            }
            if (mp != 2 || turn != 1 || dice != 1 || spawns != UnitSpawner.Instance.SpawnPoints.Count ||
                captures != (CapturePointManager.Instance != null ? CapturePointManager.Instance.CapturePoints.Count : 0))
                throw new ArgumentException("Incomplete replacement state.");
        }
    }
}
