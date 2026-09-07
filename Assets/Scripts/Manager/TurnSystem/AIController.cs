using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TurnBasedGame.Capture;
using TurnBasedGame.Unit;
using RedBjorn.ProtoTiles;
using TurnBasedGame.Resources;
using TurnBasedGame.Command;

namespace TurnBasedGame.Core
{
    /// <summary>
    /// Quản lý logic AI cho đối thủ máy.
    /// AI spawn unit, ưu tiên hạ mục tiêu yếu, chiếm cứ điểm và áp sát đối thủ.
    /// </summary>
    public class AIController : MonoBehaviour
    {
        [Header("AI Settings")]
        [SerializeField] private List<UnitController> availableUnits = new List<UnitController>();
        [SerializeField] private float actionDelay = 1f;
        [SerializeField] private int maxUnit = 3;

        private const float ImmediateAttackScore = 5000f;
        private const float LethalAttackScore = 10000f;
        private const float EnemyCaptureScore = 2500f;
        private const float NeutralCaptureScore = 2000f;
        private const float CaptureApproachScore = 1000f;
        private const float LowHealthScore = 100f;
        private const float DistancePenalty = 10f;
        private const float ActionCompletionTimeout = 10.5f;

        private PlayerID aiPlayerID;
        private PlayerID opponentOfAI;

        public void Initialize(PlayerID mainPlayer)
        {
            opponentOfAI = mainPlayer;
            aiPlayerID = mainPlayer == PlayerID.Player1 ? PlayerID.Player2 : PlayerID.Player1;
        }

        /// <summary>
        /// Thực hiện lượt chơi của AI.
        /// </summary>
        public IEnumerator ExecuteAITurn()
        {
            yield return new WaitForSeconds(actionDelay);

            if (!IsAITurnActive())
                yield break;

            MPManager.Instance.AddMP(aiPlayerID, 3);
            bool spawned = TrySpawnBestUnit();

            if (spawned)
                yield return new WaitForSeconds(actionDelay);

            var myUnits = GetLivingUnits(aiPlayerID);
            foreach (var unit in myUnits)
                unit.OnTurnBegin();

            foreach (var unit in myUnits)
            {
                if (!IsAITurnActive())
                    yield break;

                if (unit == null || unit.IsDead())
                    continue;

                yield return StartCoroutine(ExecuteUnitAction(unit));
                yield return new WaitForSeconds(actionDelay);
            }

            if (!IsAITurnActive())
                yield break;

            yield return new WaitForSeconds(actionDelay);
            foreach (var unit in myUnits)
            {
                if (unit != null && !unit.IsDead())
                    unit.FinishTurnActions();
            }

            LocalMatchAuthority.SubmitEndTurn(aiPlayerID);
        }

        /// <summary>
        /// Spawn unit mạnh nhất trong số các unit AI hiện đủ MP để mua.
        /// </summary>
        private bool TrySpawnBestUnit()
        {
            if (GetLivingUnits(aiPlayerID).Count >= maxUnit ||
                availableUnits == null || availableUnits.Count == 0 ||
                MPManager.Instance == null)
            {
                return false;
            }

            int currentMP = MPManager.Instance.GetCurrentMP(aiPlayerID);
            UnitController bestUnit = null;
            int bestScore = int.MinValue;

            foreach (var candidate in availableUnits)
            {
                if (candidate == null || candidate.UnitData == null ||
                    candidate.UnitData.spawnCost > currentMP)
                {
                    continue;
                }

                var data = candidate.UnitData;
                int score = data.BaseDamage * 4 + data.Health + data.moveRange * 8;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestUnit = candidate;
                }
            }

            if (bestUnit == null)
            {
                Debug.Log("AI không thể spawn unit: không có unit phù hợp với MP hiện tại.");
                return false;
            }

            bool success = LocalMatchAuthority.SubmitSpawn(aiPlayerID, bestUnit).Succeeded;
            if (success)
                Debug.Log($"AI đã spawn {bestUnit.UnitData.unitName}");
            else
                Debug.Log("AI không thể spawn unit: không có spawn point hợp lệ.");

            return success;
        }

        /// <summary>
        /// Tấn công mục tiêu tốt nhất; nếu chưa thể, chọn ô đi theo utility rồi thử lại.
        /// </summary>
        private IEnumerator ExecuteUnitAction(UnitController unit)
        {
            if (!unit.CanAct())
                yield break;

            var opponents = GetLivingUnits(opponentOfAI);
            var target = FindBestAttackTarget(unit, opponents);
            if (target != null)
            {
                yield return StartCoroutine(AttackTarget(unit, target));
                yield break;
            }

            yield return StartCoroutine(MoveToBestTile(unit, opponents));

            if (unit == null || unit.IsDead() || !IsAITurnActive())
                yield break;

            target = FindBestAttackTarget(unit, GetLivingUnits(opponentOfAI));
            if (target != null)
                yield return StartCoroutine(AttackTarget(unit, target));
        }

        private UnitController FindBestAttackTarget(UnitController attacker, List<UnitController> opponents)
        {
            UnitController bestTarget = null;
            float bestScore = float.MinValue;

            foreach (var target in opponents)
            {
                if (!attacker.AttackComponent.CanAttack(target))
                    continue;

                float score = ScoreTarget(attacker, target, ImmediateAttackScore);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = target;
                }
            }

            return bestTarget;
        }

        private IEnumerator MoveToBestTile(UnitController unit, List<UnitController> opponents)
        {
            if (!unit.CanMove())
                yield break;

            var map = MapManager.Instance.MapEntity;
            var currentTile = map.Tile(unit.transform.position);
            if (currentTile == null)
                yield break;

            var walkableTiles = map.WalkableTiles(currentTile.Position, unit.GetMoveRange());
            if (walkableTiles == null || walkableTiles.Count == 0)
                yield break;

            TileEntity bestTile = null;
            float bestScore = float.MinValue;

            foreach (var tile in walkableTiles)
            {
                if (!tile.Vacant || MapManager.Instance.HasUnitAtTile(tile.Position))
                    continue;

                float score = ScoreMoveTile(unit, tile.Position, opponents);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTile = tile;
                }
            }

            if (bestTile == null)
            {
                Debug.Log($"AI không tìm thấy mục tiêu di chuyển hợp lệ cho {unit.name}");
                yield break;
            }

            var path = map.PathTiles(
                unit.transform.position,
                map.WorldPosition(bestTile.Position),
                unit.GetMoveRange());

            if (path == null || path.Count == 0)
                yield break;

            Debug.Log($"AI di chuyển {unit.name} đến {bestTile.Position} (utility {bestScore:0})");
            var moveResult = LocalMatchAuthority.SubmitMove(
                aiPlayerID, unit, bestTile.Position);
            if (!moveResult.Succeeded)
                yield break;
            while (!unit.IsMoveDone() && IsAITurnActive())
                yield return null;
        }

        private float ScoreMoveTile(
            UnitController unit,
            Vector3Int tilePosition,
            List<UnitController> opponents)
        {
            float score = float.MinValue;
            float nearestDistance = float.MaxValue;

            var capturePoint = CapturePointManager.Instance?.GetPointAt(tilePosition);
            if (capturePoint != null && !capturePoint.IsCapturedBy(aiPlayerID))
                score = capturePoint.IsNeutral ? NeutralCaptureScore : EnemyCaptureScore;

            var capturePoints = CapturePointManager.Instance?.CapturePoints;
            if (capturePoints != null)
            {
                foreach (var point in capturePoints)
                {
                    if (point == null || point.IsCapturedBy(aiPlayerID))
                        continue;

                    float distance = MapManager.Instance.GetDistance(tilePosition, point.GridPosition);
                    score = Mathf.Max(score, CaptureApproachScore - distance * DistancePenalty);
                }
            }

            foreach (var target in opponents)
            {
                if (target == null || target.IsDead())
                    continue;

                float distance = MapManager.Instance.GetDistance(tilePosition, target.currentGridPosition);
                nearestDistance = Mathf.Min(nearestDistance, distance);

                if (unit.AttackComponent.CanAttackFrom(target, tilePosition))
                    score = Mathf.Max(score, ScoreTarget(unit, target, ImmediateAttackScore));
            }

            if (nearestDistance < float.MaxValue)
                score = Mathf.Max(score, -nearestDistance * DistancePenalty);

            return score;
        }

        private float ScoreTarget(UnitController attacker, UnitController target, float baseScore)
        {
            float score = baseScore + (1f - target.GetHealthPercent()) * LowHealthScore;
            if (target.GetCurrentHealth() <= attacker.GetCurrentDamage())
                score += LethalAttackScore;

            return score;
        }

        private IEnumerator AttackTarget(UnitController attacker, UnitController target)
        {
            if (attacker == null || target == null || target.IsDead())
                yield break;

            Debug.Log($"AI tấn công: {attacker.name} -> {target.name}");
            attacker.AttackComponent.ExecuteAttack(target, true);

            float elapsed = 0f;
            while (!attacker.IsActionFinished() &&
                   IsAITurnActive() &&
                   elapsed < ActionCompletionTimeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private List<UnitController> GetLivingUnits(PlayerID player)
        {
            var livingUnits = new List<UnitController>();
            var units = UnitSpawner.Instance.GetPlayerUnits(player);

            foreach (var unit in units)
            {
                if (unit != null && !unit.IsDead())
                    livingUnits.Add(unit);
            }

            return livingUnits;
        }

        private bool IsAITurnActive()
        {
            return TurnManager.Instance != null &&
                   TurnManager.Instance.CurrentState != TurnState.GameEnd &&
                   TurnManager.Instance.CurrentPlayer == aiPlayerID;
        }
    }
}
