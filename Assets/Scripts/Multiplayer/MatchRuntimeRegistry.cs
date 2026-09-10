using System;
using System.Collections.Generic;
using System.Globalization;
using TurnBasedGame.Unit;

namespace TurnBasedGame.Multiplayer
{
    // One instance per match. Clients bind the host's IDs when applying spawn results/snapshots.
    public sealed class MatchRuntimeRegistry
    {
        private readonly Dictionary<ulong, UnitController> units = new();
        private readonly Dictionary<UnitController, ulong> unitIds = new();
        private readonly Dictionary<string, SpawnPoint> spawnPoints = new(StringComparer.Ordinal);
        private readonly Dictionary<SpawnPoint, string> spawnIds = new();
        private ulong nextUnitId;

        public ulong AllocateUnit(UnitController unit)
        {
            ulong id = checked(nextUnitId + 1);
            BindUnit(id, unit);
            return id;
        }
        public void BindUnit(ulong id, UnitController unit)
        {
            if (id == 0 || unit == null || units.ContainsKey(id) || unitIds.ContainsKey(unit))
                throw new InvalidOperationException("Missing or duplicate UnitRuntimeId/reference.");
            units.Add(id, unit);
            unitIds.Add(unit, id);
            nextUnitId = Math.Max(nextUnitId, id);
        }
        public void RemoveUnit(UnitController unit)
        {
            if (ReferenceEquals(unit, null) || !unitIds.TryGetValue(unit, out var id)) return;
            unitIds.Remove(unit);
            units.Remove(id);
        }
        public ulong GetUnitId(UnitController unit) => unit != null && unitIds.TryGetValue(unit, out var id) ? id : 0;
        public UnitController ResolveUnit(ulong id) => units.TryGetValue(id, out var unit) && unit != null ? unit : null;

        public void RegisterSpawnPoints(IEnumerable<SpawnPoint> points)
        {
            var newPoints = new Dictionary<string, SpawnPoint>(StringComparer.Ordinal);
            var newIds = new Dictionary<SpawnPoint, string>();
            foreach (var point in points)
            {
                if (point == null) throw new InvalidOperationException("Missing spawn point reference.");
                var p = point.GridPosition;
                string id = string.Format(CultureInfo.InvariantCulture, "{0}:{1}:{2}:{3}", (int)point.Owner, p.x, p.y, p.z);
                if (!newPoints.TryAdd(id, point) || !newIds.TryAdd(point, id))
                    throw new InvalidOperationException($"Duplicate SpawnPointId: {id}");
            }
            spawnPoints.Clear();
            spawnIds.Clear();
            foreach (var pair in newPoints) spawnPoints.Add(pair.Key, pair.Value);
            foreach (var pair in newIds) spawnIds.Add(pair.Key, pair.Value);
        }
        public string GetSpawnPointId(SpawnPoint point) => point != null && spawnIds.TryGetValue(point, out var id) ? id : null;
        public SpawnPoint ResolveSpawnPoint(string id) => id != null && spawnPoints.TryGetValue(id, out var point) ? point : null;
    }
}
