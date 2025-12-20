using UnityEngine;
using System;
using System.Collections.Generic;
using RedBjorn.ProtoTiles;
using RedBjorn.ProtoTiles.Example;
using UnityEngine.UI;
using TurnBasedGame.Skills;

namespace TurnBasedGame.Unit
{
    /// <summary>
    /// Component xử lý logic tấn công của unit
    /// Quản lý phạm vi tấn công, target selection và thực thi damage
    /// Tích hợp với Skill System (Strategy Pattern)
    /// </summary>
    public class UnitAttack : MonoBehaviour
    {
        UnitRuntimeStats runtimeStats;
        UnitSkillManager skillManager;

        [Header("Attack Settings")]
        [SerializeField] private bool canAttackThroughObstacles = false;
        [SerializeField] private bool useSkillSystem = true;

        [Header("Attack State Button")]
        [SerializeField] Button AttackModeButton;
        [SerializeField] Button FinishAttackButton;
        [SerializeField] CanvasGroup _actionCanvasGroup;
        MapEntity _cachedMap;

        #region  Unity Methods and Initialization
        public void Init(UnitRuntimeStats runtimeStats)
        {
            this.runtimeStats = runtimeStats;
            _cachedMap = runtimeStats.MapEntity;
            
            // Khởi tạo Skill Manager
            skillManager = GetComponent<UnitSkillManager>();
            if (skillManager == null && useSkillSystem)
            {
                skillManager = gameObject.AddComponent<UnitSkillManager>();
            }
            
            if (skillManager != null)
            {
                skillManager.Initialize(GetComponent<UnitMove>());
            }
            
            AttackModeButton.onClick.AddListener(EnterAttackMode);
            FinishAttackButton.onClick.AddListener(FinishAttack);
        }

        void OnDisable()
        {
            AttackModeButton.onClick.RemoveListener(EnterAttackMode);
            FinishAttackButton.onClick.RemoveListener(FinishAttack);
        }

        void Update()
        {
            if (!runtimeStats.IsInAttackMode)
                return;

            var mousePos = MyInput.GroundPosition(_cachedMap.Settings.Plane());
            if (MyInput.GetOnWorldUp(_cachedMap.Settings.Plane()))
            {
                var tileClicked = _cachedMap.Tile(mousePos);
                if (tileClicked == null)
                    return;
                
                // Sử dụng Skill System nếu được bật
                if (useSkillSystem && skillManager != null)
                {
                    // todo: Chọn skill = UI; fix skillManager show được skill đang chọn (nếu ko chọn gì thì dùng normal)
                    var usableSkills = skillManager.GetUsableSkills(tileClicked.Position);
                    if (usableSkills.Count > 0)
                    {
                        // Ưu tiên dùng Normal Skill
                        var skillToUse = skillManager.NormalSkill != null && 
                                        skillManager.NormalSkill.CanUse(GetComponent<UnitMove>(), tileClicked.Position)
                            ? skillManager.NormalSkill
                            : usableSkills[0];
                        
                        ExecuteSkillAttack(skillToUse, tileClicked.Position);
                    }
                }
                else
                {
                    // Fallback: Dùng logic tấn công cũ
                    var unitAtTile = MapManager.Instance?.GetUnitAtTile(tileClicked.Position);
                    if (unitAtTile == null)
                        return;
                    if (CanAttack(unitAtTile))
                    {
                        ExecuteAttack(unitAtTile, true);
                    }
                }
            }
        }
        #endregion

        #region  Attack Logic
        private void FinishAttack()
        {
            ExitAttackMode();
            
            // Giảm cooldown skills khi kết thúc turn
            if (useSkillSystem && skillManager != null)
            {
                skillManager.OnTurnEnd();
            }
            
            runtimeStats.OnFinishTurn?.Invoke();
        }

        /// <summary>
        /// Bật chế độ tấn công, hiển thị vùng tấn công
        /// </summary>
        public void EnterAttackMode()
        {
            if (runtimeStats.IsInAttackMode) return;
            runtimeStats.IsInAttackMode = true;
            _actionCanvasGroup.alpha = 0;
            AreaPathManager.Instance.ShowAttackArea(
                _cachedMap.WalkableBorder(
                    _cachedMap.Tile(transform.position).Position,
                    runtimeStats.AttackRange));
        }

        /// <summary>
        /// Thực hiện tấn công vào target
        /// </summary>
        public void ExecuteAttack(UnitMove targetUnit, bool immidiate = false)
        {
            if (immidiate && !CanAttack(targetUnit))
                return;

            // Gây sát thương
            DealDamage(targetUnit);
            FinishAttack();
        }

        /// <summary>
        /// Thoát chế độ tấn công
        /// </summary>
        public void ExitAttackMode()
        {
            if (!runtimeStats.IsInAttackMode) return;
            runtimeStats.IsInAttackMode = false;
            _actionCanvasGroup.alpha = 1;
            AreaPathManager.Instance.HideAttackArea();
        }

        /// <summary>
        /// Thực thi skill attack (Skill System)
        /// </summary>
        private void ExecuteSkillAttack(ISkill skill, Vector3Int targetPos)
        {
            if (skillManager.UseSkill(skill, targetPos))
            {
                FinishAttack();
            }
        }
        #endregion

        #region  Helper Methods
        public bool CanAttack(UnitMove targetUnit)
        {
            if (targetUnit == null || targetUnit.IsDead())
                return false;

            if (targetUnit.GetOwner() == runtimeStats.Owner)
                return false;

            if (GetDistanceToTarget(targetUnit.currentGridPosition) > runtimeStats.AttackRange)
                return false;

            // Kiểm tra line of sight nếu cần
            if (!canAttackThroughObstacles)
            {
                return HasLineOfSight(targetUnit.currentGridPosition);
            }
            return true;
        }

        /// <summary>
        /// Lấy danh sách các unit có thể tấn công trong phạm vi
        /// </summary>
        public List<UnitMove> GetAttackableTargets()
        {
            var targets = new List<UnitMove>();
            var tilesInRange = GetTilesInAttackRange();

            foreach (var tile in tilesInRange)
            {
                var unitAtTile = MapManager.Instance?.GetUnitAtTile(tile.Position);
                if (unitAtTile == null) continue;

                if (unitAtTile.GetOwner() == runtimeStats.Owner) continue;

                if (canAttackThroughObstacles || HasLineOfSight(tile.Position))
                {
                    targets.Add(unitAtTile);
                }
            }

            return targets;
        }

        private List<TileEntity> GetTilesInAttackRange()
        {
            if (_cachedMap == null) return new List<TileEntity>();

            var myTile = _cachedMap.Tile(transform.position);
            if (myTile == null) return new List<TileEntity>();

            var walkableTiles = _cachedMap.WalkableTiles(myTile.Position, runtimeStats.AttackRange);
            return new List<TileEntity>(walkableTiles);
        }

        private float GetDistanceToTarget(Vector3Int targetGridPos)
        {
            var myTile = _cachedMap.Tile(transform.position);
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
            Debug.Log($"{gameObject.name} tấn công {target.name} gây {runtimeStats.AttackDamage} sát thương!");
            target.TakeDamage(runtimeStats.AttackDamage);
        }
    }
    #endregion
}
