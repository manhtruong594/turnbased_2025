using System.Collections.Generic;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.Skills
{
    public static class TeleportUtility
    {
        public static bool TryFindAdjacentTileAroundTarget(
            UnitController caster,
            UnitController target,
            out Vector3Int teleportTile)
        {
            teleportTile = Vector3Int.zero;
            if (caster == null || target == null)
                return false;

            foreach (var candidate in GetOrderedCandidates(caster, target))
            {
                if (!IsValidTeleportTile(candidate))
                    continue;

                teleportTile = candidate;
                return true;
            }

            return false;
        }

        private static List<Vector3Int> GetOrderedCandidates(UnitController caster, UnitController target)
        {
            var candidates = new List<Vector3Int>();
            Vector3Int attackDirection = SkillAreaUtility.NormalizeDirection(
                target.currentGridPosition - caster.currentGridPosition);

            if (attackDirection != Vector3Int.zero)
                candidates.Add(target.currentGridPosition + attackDirection);

            foreach (var adjacentTile in SkillAreaUtility.GetAdjacentTiles(target.currentGridPosition))
            {
                if (!candidates.Contains(adjacentTile))
                    candidates.Add(adjacentTile);
            }

            return candidates;
        }

        private static bool IsValidTeleportTile(Vector3Int tile)
        {
            return MapManager.Instance != null && MapManager.Instance.IsTileAvailable(tile);
        }
    }
}
