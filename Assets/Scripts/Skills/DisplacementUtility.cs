using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.Skills
{
    public readonly struct DisplacementResult
    {
        public bool WasMoved { get; }
        public bool WasBlocked { get; }
        public bool WasImmune { get; }

        private DisplacementResult(bool wasMoved, bool wasBlocked, bool wasImmune)
        {
            WasMoved = wasMoved;
            WasBlocked = wasBlocked;
            WasImmune = wasImmune;
        }

        public static DisplacementResult Moved() => new(true, false, false);
        public static DisplacementResult Blocked() => new(false, true, false);
        public static DisplacementResult Immune() => new(false, false, true);
    }

    public static class DisplacementUtility
    {
        public static DisplacementResult TryPushUnit(UnitController unit, Vector3Int direction, int distance)
        {
            if (unit == null || unit.IsDead())
                return DisplacementResult.Blocked();

            if (unit.BuffHandler != null && unit.BuffHandler.IsImmuneToDisplacement())
                return DisplacementResult.Immune();

            Vector3Int normalizedDirection = SkillAreaUtility.NormalizeDirection(direction);
            if (normalizedDirection == Vector3Int.zero || distance <= 0)
                return DisplacementResult.Blocked();

            Vector3Int destination = unit.currentGridPosition;
            for (int step = 0; step < distance; step++)
            {
                Vector3Int nextTile = destination + normalizedDirection;
                if (!CanMoveInto(nextTile))
                    return destination == unit.currentGridPosition
                        ? DisplacementResult.Blocked()
                        : MoveUnit(unit, destination);

                destination = nextTile;
            }

            return MoveUnit(unit, destination);
        }

        private static bool CanMoveInto(Vector3Int tilePos)
        {
            return MapManager.Instance != null && MapManager.Instance.IsTileAvailable(tilePos);
        }

        private static DisplacementResult MoveUnit(UnitController unit, Vector3Int destination)
        {
            unit.TeleportTo(destination);
            return DisplacementResult.Moved();
        }
    }
}
