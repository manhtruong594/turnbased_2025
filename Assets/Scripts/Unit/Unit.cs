using UnityEngine;
using TurnBasedGame.Core;
using RedBjorn.ProtoTiles.Example;
using System;

namespace TurnBasedGame.Unit
{
    /// <summary>
    /// Component chính cho mỗi unit trong game
    /// Quản lý thông tin runtime và tương tác với grid system
    /// </summary>
    public class Unit : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private UnitData unitData;
        public UnitMove MoveComponent;
        public UnitAttack AttackComponent;
        
        // Runtime Properties
        private PlayerID ownerID;

        // Properties
        public UnitData Data => unitData;
        public PlayerID Owner => ownerID;

        void OnDisable()
        {
            MoveComponent.SelectAction -= Select;
            MoveComponent.DeselectAction -= Deselect;
        }

        /// <summary>
        /// Khởi tạo unit với data và owner
        /// </summary>
        public void Initialize(UnitData data, PlayerID owner, Vector3Int gridPos)
        {
            unitData = data;
            MoveComponent.Init(MapManager.Instance.MapEntity, gridPos);
            ownerID = owner;
            MoveComponent.SelectAction += Select;
            MoveComponent.DeselectAction += Deselect;

            // Khởi tạo AttackComponent nếu có
            if (AttackComponent != null)
            {
                AttackComponent.Init(MapManager.Instance.MapEntity, MoveComponent);
            }
            ApplyOwnerVisual();
        }
        
        public void ResetComponents()
        {
            MoveComponent.ResetMove();
        }

        /// <summary>
        /// Chọn unit này
        /// </summary>
        public void Select()
        {
            ShowSelectionFeedback(true);
            AttackComponent.EnterAttackMode();

        }

        /// <summary>
        /// Bỏ chọn unit này
        /// </summary>
        public void Deselect()
        {
            ShowSelectionFeedback(false);
            AttackComponent.ExitAttackMode();
        }
 
        private void ApplyOwnerVisual()
        {
            // TODO: Thêm logic đổi màu/material theo team
            // Ví dụ: Player1 = Blue, Player2 = Red
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                // Placeholder - sẽ cải thiện sau với proper materials
                Color teamColor = ownerID == PlayerID.Player1 ? Color.blue : Color.red;
                renderer.material.color = teamColor;
            }
        }

        private void ShowSelectionFeedback(bool show)
        {
            // TODO: Thêm visual feedback khi unit được chọn
            // Ví dụ: Highlight outline, particle effect, scale animation
            if (show)
            {
                transform.localScale = Vector3.one * 1.1f;
            }
            else
            {
                transform.localScale = Vector3.one;
            }
        }

        public void RequireAttack(Vector3Int targetGridPos)
        {
            if (AttackComponent != null)
            {
                AttackComponent.ExecuteAttack(targetGridPos);
            }
        }

        public void AttackImmidiate(Vector3Int targetGridPos)
        {
            if (AttackComponent != null)
            {
                AttackComponent.ExecuteAttack(targetGridPos, true);
            }
        }

        public bool CheckAttackPossible(Vector3Int targetGridPos)
        {
            if (AttackComponent != null)
            {
                return AttackComponent.CanAttack(targetGridPos);
            }
            return false;
        }
    }
}
