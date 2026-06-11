using System.Collections.Generic;
using TurnBasedGame.Core;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.Skills
{
    public static class SkillAreaUtility
    {
        public static List<Vector3Int> GetRadiusTiles(Vector3Int center, int radius)
        {
            var result = new List<Vector3Int>();
            var map = MapManager.Instance?.MapEntity;
            if (map == null)
                return result;

            foreach (var tilePos in map.Area(center, Mathf.Max(0, radius)))
            {
                if (map.Tile(tilePos) != null)
                    result.Add(tilePos);
            }

            return result;
        }

        public static List<Vector3Int> GetLineTiles(Vector3Int origin, Vector3Int direction, int length)
        {
            var result = new List<Vector3Int>();
            var map = MapManager.Instance?.MapEntity;
            Vector3Int normalizedDirection = NormalizeDirection(direction);
            if (map == null || normalizedDirection == Vector3Int.zero)
                return result;

            for (int step = 1; step <= length; step++)
            {
                Vector3Int tilePos = origin + normalizedDirection * step;
                if (map.Tile(tilePos) != null)
                    result.Add(tilePos);
            }

            return result;
        }

        public static List<Vector3Int> GetAdjacentTiles(Vector3Int center)
        {
            var tiles = GetRadiusTiles(center, 1);
            tiles.Remove(center);
            return tiles;
        }

        public static List<UnitController> GetEnemiesInTiles(PlayerID casterOwner, IEnumerable<Vector3Int> tiles)
        {
            var enemies = new List<UnitController>();
            foreach (var tile in tiles)
            {
                var unit = MapManager.Instance?.GetUnitAtTile(tile);
                if (unit != null && !unit.IsDead() && unit.GetOwner() != casterOwner)
                    enemies.Add(unit);
            }

            return enemies;
        }

        public static UnitController GetFirstEnemyInLine(PlayerID casterOwner, Vector3Int origin, Vector3Int direction, int length)
        {
            foreach (var tile in GetLineTiles(origin, direction, length))
            {
                var unit = MapManager.Instance?.GetUnitAtTile(tile);
                if (unit != null && !unit.IsDead() && unit.GetOwner() != casterOwner)
                    return unit;
            }

            return null;
        }

        public static Vector3Int NormalizeDirection(Vector3Int direction)
        {
            if (direction == Vector3Int.zero)
                return Vector3Int.zero;

            return new Vector3Int(
                Mathf.Clamp(direction.x, -1, 1),
                Mathf.Clamp(direction.y, -1, 1),
                Mathf.Clamp(direction.z, -1, 1));
        }
    }
}
