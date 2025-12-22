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
        //[SerializeField] Button AttackModeButton;
        [SerializeField] Button FinishAttackButton;
        [SerializeField] CanvasGroup _actionCanvasGroup;

        [Header("Skill System")]
        [SerializeField] private List<SkillBase> startingSkills = new List<SkillBase>();
        private List<ISkill> activeSkills = new List<ISkill>();
        [SerializeField] private SkillButton skillButtonPrefab;
        [SerializeField] private Transform skillButtonContainer;

        private SkillBase _normalSkill;
        private SkillBase _selectedSkill;

        MapEntity _cachedMap;

        #region  Unity Methods and Initialization
        public void Init(UnitRuntimeStats runtimeStats)
        {
            this.runtimeStats = runtimeStats;
            _cachedMap = runtimeStats.MapEntity;

            FinishAttackButton.onClick.AddListener(FinishAttack);
            _normalSkill = startingSkills[0];
            _selectedSkill = _normalSkill;


            {
                activeSkills.Clear();
                foreach (var skill in startingSkills)
                {
                    activeSkills.Add(skill.Clone());
                }
                foreach (var skill in startingSkills)
                {
                    var skillBtnObj = Instantiate(skillButtonPrefab, skillButtonContainer);
                    skillBtnObj.Initialize(skill, OnSkillButtonClicked);
                }
            }
        }

        private void OnSkillButtonClicked(ISkill skill)
        {
            _selectedSkill = skill as SkillBase;
            EnterAttackMode();
        }

        void OnDisable()
        {
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
                if (_selectedSkill.CanUse(this.GetComponent<UnitMove>(), tileClicked.Position))
                {
                    // Sử dụng skill đã chọn
                    _selectedSkill.Execute(this.GetComponent<UnitMove>(), tileClicked.Position);
                    return;
                }
                else
                {
                    ExitAttackMode();
                    Debug.Log("Cannot use skill on this tile.");
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
                    _selectedSkill.Range));
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

            if (GetDistanceToTarget(targetUnit.currentGridPosition) > _selectedSkill.Range)
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

            var walkableTiles = _cachedMap.WalkableTiles(myTile.Position, _selectedSkill.Range);
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
            Debug.Log($"{gameObject.name} tấn công {target.name} gây {_selectedSkill.BaseValue} sát thương!");
            target.TakeDamage(_selectedSkill.BaseValue);
        }
    }
    #endregion
}
