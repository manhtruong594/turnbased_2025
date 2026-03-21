using UnityEngine;
using System;
using System.Collections.Generic;
using RedBjorn.ProtoTiles;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TurnBasedGame.Unit;
using TurnBasedGame.Skills;
using RedBjorn.ProtoTiles.Example;

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
        UnitSkillsBridge _skillsBridge = new UnitSkillsBridge();

        [Header("Attack Settings")]
        [SerializeField] private bool canAttackThroughObstacles = false;

        [Header("Attack State Button")]
        [SerializeField] Button FinishAttackButton;
        [SerializeField] CanvasGroup _actionCanvasGroup;

        [Header("Skill System")]
        [SerializeField] private List<SkillBase> startingSkills = new List<SkillBase>();
        [SerializeField] private GameObject skillButtonPrefab;
        [SerializeField] private Transform skillButtonContainer;

        private List<ISkill> activeSkills = new List<ISkill>();
        private ISkill _normalSkill;
        private ISkill _selectedSkill;

        MapEntity _cachedMap;
        UnitController _cachedUnitMove;

        #region  Unity Methods and Initialization
        public void Init(UnitRuntimeStats runtimeStats, UnitController unitMove)
        {
            this.runtimeStats = runtimeStats;
            _cachedMap = runtimeStats.MapEntity;
            _cachedUnitMove = unitMove;
            FinishAttackButton.onClick.AddListener(FinishAttack);

            {
                activeSkills.Clear();
                foreach (var skill in startingSkills)
                {
                    var iSkill = skill.Clone();
                    iSkill.ResetCooldown();
                    activeSkills.Add(iSkill);
                    _skillsBridge.CreateSkillButton(skillButtonPrefab, skillButtonContainer, iSkill, OnSkillButtonClicked);
                }
                _normalSkill = activeSkills[0];
                _selectedSkill = _normalSkill;
            }
        }

        private void OnSkillButtonClicked(ISkill skill)
        {
            _selectedSkill = skill;
            EnterAttackMode();
        }

        void OnDisable()
        {
            FinishAttackButton.onClick.RemoveListener(FinishAttack);
            _skillsBridge.DisposeSkillButtons();
        }

        void Update()
        {
            if (!runtimeStats.IsInAttackMode)
                return;

            var mousePos = MyInput.GroundPosition(_cachedMap.Settings.Plane());
            if (MyInput.GetOnWorldUp(_cachedMap.Settings.Plane()) && !EventSystem.current.IsPointerOverGameObject())
            {
                var tileClicked = _cachedMap.Tile(mousePos);
                if (tileClicked == null)
                    return;
                if (_selectedSkill.CanUse(_cachedUnitMove, tileClicked.Position))
                {
                    // Sử dụng skill đã chọn
                    _selectedSkill.Execute(_cachedUnitMove, tileClicked.Position);
                    return;
                }
                else
                {
                    AreaPathManager.Instance.RealeaseSelectedUnit();
                    Debug.Log("Cannot use skill on this tile.");
                }
            }
        }

        #endregion

        #region  Attack Logic
        public void FinishAttack()
        {
             _cachedUnitMove.FinishTurnActions();
        }

        /// <summary>
        /// Bật chế độ tấn công, hiển thị vùng tấn công
        /// </summary>
        public void EnterAttackMode()
        {
            if (runtimeStats.IsInAttackMode) return;
            runtimeStats.IsInAttackMode = true;
            _actionCanvasGroup.alpha = 0;
            _actionCanvasGroup.interactable = false;
            _actionCanvasGroup.blocksRaycasts = false;
            AreaPathManager.Instance.ShowAttackArea(
                _cachedMap.WalkableBorder(
                    _cachedMap.Tile(transform.position).Position,
                    _selectedSkill.Range));
        }

        /// <summary>
        /// Thực hiện tấn công vào target
        /// </summary>
        public void ExecuteAttack(UnitController targetUnit, bool immidiate = false)
        {
            if (immidiate && !CanAttack(targetUnit))
                return;

            _selectedSkill.Execute(_cachedUnitMove, targetUnit.currentGridPosition);
        }

        /// <summary>
        /// Thoát chế độ tấn công
        /// </summary>
        public void ExitAttackMode()
        {
            if (!runtimeStats.IsInAttackMode) return;
            runtimeStats.IsInAttackMode = false;
            _actionCanvasGroup.alpha = 1;
            _actionCanvasGroup.interactable = true;
            _actionCanvasGroup.blocksRaycasts = true;
            AreaPathManager.Instance.HideAttackArea();
        }

        #endregion

        #region  Helper Methods
        public void ReduceSkillsCooldowns()
        {
            foreach (var skill in activeSkills)
            {
                skill.ReduceCooldown();
            }
            _skillsBridge.UpdateSkillButtonsCooldowns();
        }

        public bool CanAttack(UnitController targetUnit)
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
    }
    #endregion
}
