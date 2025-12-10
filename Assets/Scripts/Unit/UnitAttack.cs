using UnityEngine;
using System;
using System.Collections.Generic;
using RedBjorn.ProtoTiles;
using RedBjorn.ProtoTiles.Example;

namespace TurnBasedGame.Unit
{
    /// <summary>
    /// Component xử lý logic tấn công của unit
    /// Quản lý phạm vi tấn công, target selection và thực thi damage
    /// </summary>
    public class UnitAttack : MonoBehaviour
    {
        [Header("Attack Settings")]
        [Tooltip("Phạm vi tấn công (số ô)")]
        [SerializeField] private float attackRange = 2f;
        
        [Tooltip("Sát thương gây ra")]
        [SerializeField] private int attackDamage = 10;
        
        [Tooltip("Có thể tấn công xuyên qua chướng ngại vật không")]
        [SerializeField] private bool canAttackThroughObstacles = false;

        [Header("Visual")]
        [Tooltip("Prefab hiển thị vùng tấn công")]
        [SerializeField] private AreaOutline attackAreaPrefab;

        private MapEntity cachedMap;
        private AreaOutline attackArea;
        private bool isAttackMode = false;

        public float AttackRange => attackRange;
        public int AttackDamage => attackDamage;
        public bool IsAttackMode => isAttackMode;

        /// <summary>
        /// Khởi tạo component attack
        /// </summary>
        public void Init(MapEntity map)
        {
            cachedMap = map;
            
            if (attackAreaPrefab != null)
            {
                attackArea = Instantiate(attackAreaPrefab, Vector3.zero, Quaternion.identity);
                attackArea.Hide();
            }
        }

        /// <summary>
        /// Bật chế độ tấn công, hiển thị vùng tấn công
        /// </summary>
        public void EnterAttackMode()
        {
            if (isAttackMode) return;

            isAttackMode = true;
            ShowAttackRange();
        }

        /// <summary>
        /// Thoát chế độ tấn công
        /// </summary>
        public void ExitAttackMode()
        {
            if (!isAttackMode) return;
            isAttackMode = false;
            HideAttackRange();
        }

        /// <summary>
        /// Kiểm tra có thể tấn công target tại vị trí này không
        /// </summary>
        public bool CanAttack(Vector3Int targetGridPos)
        {
            var distance = GetDistanceToTarget(targetGridPos);
            if (distance > attackRange) return false;

            // Kiểm tra có target tại vị trí này không
            var targetUnit = MapManager.Instance?.GetUnitAtTile(targetGridPos);
            if (targetUnit == null) return false;

            // Không thể tấn công chính mình hoặc đồng đội
            var myUnit = GetComponent<UnitMove>();
            if (myUnit != null && targetUnit.GetComponent<UnitMove>() != null)
            {
                var target = targetUnit.GetComponent<UnitMove>();
                if (target.Owner == myUnit.Owner) return false;
            }

            // Kiểm tra line of sight nếu cần
            if (!canAttackThroughObstacles)
            {
                return HasLineOfSight(targetGridPos);
            }

            return true;
        }

        /// <summary>
        /// Thực hiện tấn công vào target
        /// </summary>
        public void ExecuteAttack(Vector3Int targetGridPos, bool immidiate = false)
        {
            if (immidiate && !CanAttack(targetGridPos)) 
                return;

            var targetUnitMove = MapManager.Instance?.GetUnitAtTile(targetGridPos);
            if (targetUnitMove == null) return;

            // var target = targetUnitMove.GetUnit();
            // if (target == null) return;

            // // TODO: Thêm animation tấn công
            // // TODO: Thêm sound effect

            // // Gây sát thương
            // DealDamage(target);

            // // Kết thúc chế độ tấn công
            // ExitAttackMode();
        }

        /// <summary>
        /// Lấy danh sách các unit có thể tấn công trong phạm vi
        /// </summary>
        public List<UnitMove> GetAttackableTargets()
        {
            var targets = new List<UnitMove>();
            var myUnit = GetComponent<UnitMove>();
            if (myUnit == null) return targets;

            var tilesInRange = GetTilesInAttackRange();
            
            foreach (var tile in tilesInRange)
            {
                var unitAtTile = MapManager.Instance?.GetUnitAtTile(tile.Position);
                if (unitAtTile == null) continue;

                var targetUnit = unitAtTile.GetComponent<UnitMove>();
                if (targetUnit == null) continue;
                if (targetUnit.Owner == myUnit.Owner) continue;

                if (canAttackThroughObstacles || HasLineOfSight(tile.Position))
                {
                    targets.Add(targetUnit);
                }
            }

            return targets;
        }

        private void ShowAttackRange()
        {
            if (attackArea == null || cachedMap == null) return;

            var borderPoints = cachedMap.WalkableBorder(transform.position, attackRange);
            attackArea.Show(borderPoints, cachedMap);
            attackArea.ActiveState();
        }

        private void HideAttackRange()
        {
            if (attackArea == null) return;
            attackArea.Hide();
            attackArea.InactiveState();
        }

        private List<TileEntity> GetTilesInAttackRange()
        {
            if (cachedMap == null) return new List<TileEntity>();
            
            var myTile = cachedMap.Tile(transform.position);
            if (myTile == null) return new List<TileEntity>();
            
            var walkableTiles = cachedMap.WalkableTiles(myTile.Position, attackRange);
            return new List<TileEntity>(walkableTiles);
        }

        private float GetDistanceToTarget(Vector3Int targetGridPos)
        {
            var myTile = cachedMap.Tile(transform.position);
            if (myTile == null) return float.MaxValue;

            // Tính khoảng cách Manhattan (grid-based)
            return Mathf.Abs(myTile.Position.x - targetGridPos.x) + 
                   Mathf.Abs(myTile.Position.y - targetGridPos.y) + 
                   Mathf.Abs(myTile.Position.z - targetGridPos.z);
        }

        private bool HasLineOfSight(Vector3Int targetGridPos)
        {
            // TODO: Implement proper line of sight check
            // Hiện tại chỉ return true
            return true;
        }

        private void DealDamage(UnitMove target)
        {
            // TODO: Implement proper health/damage system
            // Hiện tại chỉ log để test
            Debug.Log($"{gameObject.name} tấn công {target.name} gây {attackDamage} sát thương!");
            
            // Placeholder - sẽ thay bằng health system sau
            Destroy(target.gameObject);
        }

        private void OnDestroy()
        {
            if (attackArea != null)
            {
                Destroy(attackArea.gameObject);
            }
        }
    }
}
