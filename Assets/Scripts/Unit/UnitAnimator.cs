using UnityEngine;
using System;
using TurnBasedGame.Skills;
using TurnBasedGame.ObjectPool;
using TurnBasedGame.VFX;

namespace TurnBasedGame.Unit
{
    /// <summary>
    /// Component quản lý animation của unit
    /// Sử dụng Observer Pattern thông qua Events
    /// </summary>
    public class UnitAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;

        [Header("Animation Settings")]
        [SerializeField] private float defaultAttackSpeed = 1f;

        // Animation Parameter Hashes (tối ưu performance)
        private static readonly int IsMoving = Animator.StringToHash("IsMoving");
        private static readonly int Attack = Animator.StringToHash("Attack");
        private static readonly int Hit = Animator.StringToHash("Hit");
        private static readonly int Death = Animator.StringToHash("Death");
        private static readonly int AttackSpeed = Animator.StringToHash("AttackSpeed");
        private static readonly int AttackType = Animator.StringToHash("AttackType");

        // Events
        public Action OnAttackHitFrame;
        public Action OnAttackAnimationComplete;
        public Action OnDeathAnimationComplete;

        SkillBase _currentSkill;
        
        #region Animation Control

        public void StartMoving()
        {
            if (animator == null) return;
            animator.SetBool(IsMoving, true);
        }

        public void StopMoving()
        {
            if (animator == null) return;
            animator.SetBool(IsMoving, false);
        }

        public void PlayAttack(SkillBase skill, Action onHitAction, float attackSpeed = -1f)
        {
            if (animator == null) return;

            if (attackSpeed < 0)
                attackSpeed = defaultAttackSpeed;
            OnAttackHitFrame = onHitAction;
            animator.SetFloat(AttackSpeed, attackSpeed);
            animator.SetTrigger(Attack);
            animator.SetInteger(AttackType, (int)skill.Type);
            _currentSkill = skill;
        }

        public void PlayHit()
        {
            if (animator == null) return;
            animator.SetTrigger(Hit);
        }

        public void PlayDeath()
        {
            if (animator == null) return;
            animator.SetTrigger(Death);
        }

        public void ResetToIdle()
        {
            if (animator == null) return;

            animator.SetBool(IsMoving, false);
            animator.ResetTrigger(Attack);
            animator.ResetTrigger(Hit);
            animator.ResetTrigger(Death);
        }

        #endregion

        #region Animation Events (Được gọi từ Animation Clips)

        /// <summary>
        /// Animation Event: Được gọi tại frame attack thực sự gây damage
        /// Thêm event này vào Attack Animation Clip tại frame hit
        /// </summary>
        public void AnimEvent_AttackHit()
        {
            OnAttackHitFrame?.Invoke();
        }

        public void AnimEvent_AttackStart()
        {
            if (_currentSkill != null)
            {
                var projectile = ObjectPoolManager.Instance.Spawn(_currentSkill.VfxPrefab).GetComponent<Projectile>();
                projectile.Launch(transform.position, transform.position + transform.forward * 10);
            }
        }

        /// <summary>
        /// Animation Event: Được gọi khi attack animation hoàn thành
        /// Thêm event này vào Attack Animation Clip tại frame cuối
        /// </summary>
        public void AnimEvent_AttackComplete()
        {
            OnAttackAnimationComplete?.Invoke();
        }

        /// <summary>
        /// Animation Event: Được gọi khi death animation hoàn thành
        /// Thêm event này vào Death Animation Clip tại frame cuối
        /// </summary>
        public void AnimEvent_DeathComplete()
        {
            OnDeathAnimationComplete?.Invoke();
        }

        #endregion
    }
}
