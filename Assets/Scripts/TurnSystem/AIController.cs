using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TurnBasedGame.Unit;
using RedBjorn.ProtoTiles;
using RedBjorn.ProtoTiles.Example;
using TurnBasedGame.Resources;

namespace TurnBasedGame.Core
{
    /// <summary>
    /// Quản lý logic AI cho đối thủ máy
    /// AI sẽ spawn unit, di chuyển và tấn công player
    /// </summary>
    public class AIController : MonoBehaviour
    {
        [Header("AI Settings")]
        [SerializeField] private List<UnitData> availableUnits = new List<UnitData>();
        [SerializeField] private float actionDelay = 1f;

        private PlayerID aiPlayerID;
        private PlayerID opponentOfAI;

        public void Initialize(PlayerID mainPlayer)
        {
            opponentOfAI = mainPlayer;
            aiPlayerID = mainPlayer == PlayerID.Player1 ? PlayerID.Player2 : PlayerID.Player1;
        }

        /// <summary>
        /// Thực hiện lượt chơi của AI
        /// </summary>
        public IEnumerator ExecuteAITurn()
        {
            yield return new WaitForSeconds(actionDelay);

            MPManager.Instance.AddMP(aiPlayerID, 3); // Cộng MP cho AI mỗi lượt
            bool spawned = TrySpawnRandomUnit();

            if (spawned)
            {
                yield return new WaitForSeconds(actionDelay);
            }

            // Bước 2: Lấy tất cả units của AI
            var myUnits = UnitSpawner.Instance.GetPlayerUnits(aiPlayerID);

            // Bước 3: Với mỗi unit, thử di chuyển gần player và tấn công
            foreach (var unit in myUnits)
            {
                if (unit == null) continue;

                yield return StartCoroutine(ExecuteUnitAction(unit));
                yield return new WaitForSeconds(actionDelay);
            }

            // Kết thúc lượt
            yield return new WaitForSeconds(actionDelay);
            TurnManager.Instance.EndCurrentTurn();
        }

        /// <summary>
        /// Spawn một unit ngẫu nhiên nếu có thể
        /// </summary>
        private bool TrySpawnRandomUnit()
        {
            if (availableUnits == null || availableUnits.Count == 0)
            {
                Debug.LogWarning("AI không có unit để spawn!");
                return false;
            }

            // Chọn random unit
            var randomUnit = availableUnits[Random.Range(0, availableUnits.Count)];

            // Thử spawn
            bool success = UnitSpawner.Instance.SpawnUnit(randomUnit, aiPlayerID);

            if (success)
            {
                Debug.Log($"AI đã spawn {randomUnit.unitName}");
            }
            else
            {
                Debug.Log("AI không thể spawn unit (không đủ MP hoặc không có spawn point)");
            }

            return success;
        }

        /// <summary>
        /// Thực hiện hành động cho 1 unit
        /// </summary>
        private IEnumerator ExecuteUnitAction(Unit.Unit unit)
        {
            if (unit == null || unit.MoveComponent == null) yield break;

            // Tìm unit gần nhất của đối thủ
            var targetUnit = FindNearestOpponentUnit(unit);

            if (targetUnit == null)
            {
                Debug.Log($"AI unit {unit.name} không tìm thấy mục tiêu");
                yield break;
            }

            if (CanAttackTarget(unit, targetUnit))
            {
                yield return StartCoroutine(AttackTarget(unit, targetUnit));
            }
            else
            {
                // Di chuyển về phía target
                yield return StartCoroutine(MoveTowardsTarget(unit, targetUnit));

                // Sau khi di chuyển, kiểm tra lại có thể tấn công không
                yield return new WaitForSeconds(0.5f);

                if (CanAttackTarget(unit, targetUnit))
                {
                    yield return StartCoroutine(AttackTarget(unit, targetUnit));
                }
            }
        }

        /// <summary>
        /// Tìm unit gần nhất của đối thủ
        /// </summary>
        private Unit.Unit FindNearestOpponentUnit(Unit.Unit myUnit)
        {
            var opponentUnits = UnitSpawner.Instance.GetPlayerUnits(opponentOfAI);

            if (opponentUnits == null || opponentUnits.Count == 0)
                return null;

            Unit.Unit nearest = null;
            float minDistance = float.MaxValue;

            foreach (var opponent in opponentUnits)
            {
                if (opponent == null) continue;

                float distance = Vector3.Distance(myUnit.transform.position, opponent.transform.position);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = opponent;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Kiểm tra có thể tấn công target không
        /// </summary>
        private bool CanAttackTarget(Unit.Unit attacker, Unit.Unit target)
        {
            var targetGridPos = MapManager.Instance.MapEntity.Tile(target.transform.position)?.Position;

            if (!targetGridPos.HasValue)
                return false;

            return attacker.CheckAttackPossible(targetGridPos.Value);
        }

        /// <summary>
        /// Di chuyển về phía target
        /// </summary>
        private IEnumerator MoveTowardsTarget(Unit.Unit myUnit, Unit.Unit target)
        {
            if (myUnit.MoveComponent == null) yield break;

            var map = MapManager.Instance.MapEntity;
            var myPos = myUnit.transform.position;
            var targetPos = target.transform.position;

            // Lấy vị trí grid của target để loại trừ
            var targetTile = map.Tile(targetPos);
            if (targetTile == null)
            {
                Debug.LogWarning("Không tìm thấy tile của target");
                yield break;
            }
            var targetGridPos = targetTile.Position;

            // Lấy các tile có thể đi được
            var walkableTiles = map.WalkableTiles(map.Tile(myPos).Position, myUnit.MoveComponent.Range);

            if (walkableTiles == null || walkableTiles.Count == 0)
            {
                Debug.Log("Không có tile nào để di chuyển");
                yield break;
            }

            // Tìm tile gần target nhất, nhưng KHÔNG phải tile của target
            TileEntity bestTile = null;
            float minDistance = float.MaxValue;

            foreach (var tile in walkableTiles)
            {
                // Bỏ qua tile không trống
                if (!tile.Vacant) continue;

                // Bỏ qua tile của target (quan trọng!)
                if (tile.Position == targetGridPos) continue;

                var tileWorldPos = map.WorldPosition(tile.Position);
                float distance = Vector3.Distance(tileWorldPos, targetPos);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestTile = tile;
                }
            }

            if (bestTile != null)
            {
                // Tính đường đi
                var path = map.PathTiles(myPos, map.WorldPosition(bestTile.Position), myUnit.MoveComponent.Range);

                if (path != null && path.Count > 0)
                {
                    Debug.Log($"AI di chuyển {myUnit.name} đến gần {target.name}");
                    myUnit.MoveComponent.Move(path);
                    while (!myUnit.MoveComponent.IsMoveCompleted)
                    {
                        yield return null;
                    }
                }
            }
            else
            {
                Debug.Log($"AI không tìm thấy tile hợp lệ để tiến gần {target.name}");
            }
        }

        /// <summary>
        /// Tấn công target
        /// </summary>
        private IEnumerator AttackTarget(Unit.Unit attacker, Unit.Unit target)
        {
            var targetGridPos = MapManager.Instance.MapEntity.Tile(target.transform.position)?.Position;
            Debug.Log($"AI tấn công: {attacker.name} -> {target.name}");
            attacker.AttackImmidiate(targetGridPos.Value);
            yield return new WaitForSeconds(0.5f);
        }
    }
}
