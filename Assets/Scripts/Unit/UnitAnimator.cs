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
        [SerializeField] private Transform _effectSpawnPoint;
        [SerializeField] private float _transitionDuration = 0.1f;

        SkillBase _currentSkill;

        #region Animation Control

        public void StartMoving()
        {
            if (animator == null) return;
            animator.SetBool(AnimationHashLib.IsMoving, true);
        }

        public void StopMoving()
        {
            if (animator == null) return;
            animator.SetBool(AnimationHashLib.IsMoving, false);
        }

        public void PlayAttack(SkillBase skill, float attackSpeed = -1f)
        {
            if (animator == null) return;

            if (attackSpeed < 0)
                attackSpeed = defaultAttackSpeed;
            //animator.SetFloat(AnimationHashLib.AttackSpeed, attackSpeed);
            animator.CrossFadeInFixedTime(AnimationHashLib.GetHashAnimByAttackType(skill.Type), _transitionDuration);
            _currentSkill = skill;
        }

        public void PlayHit()
        {
            if (animator == null) return;
            animator.CrossFadeInFixedTime(AnimationHashLib.Hit, _transitionDuration);
        }

        public void PlayDeath()
        {
            if (animator == null) return;
            animator.CrossFadeInFixedTime(AnimationHashLib.Death, _transitionDuration);
        }

        #endregion

        /// <summary>
        /// event gọi khi attack trúng target, hoặc khi skill buff kích hoạt hiệu ứng
        /// </summary>
        private void OnApplyEffect()
        {
            if (_currentSkill != null)
            {
                _currentSkill.ApplyEffect();
            }
        }

        #region Animation Events (Được gọi từ Animation Clips)

        /// <summary>
        /// Animation Event: Được gọi tại frame attack thực sự gây damage
        /// Thêm event này vào Attack Animation Clip tại frame hit
        /// </summary>
        public void AnimEvent_AttackHit()
        {
            OnApplyEffect();
        }

        /// <summary>
        /// Animation Event: Được gọi tại frame mà skill có projectile bắn ra
        /// </summary>
        public void AnimEvent_AttackStart()
        {
            if (_currentSkill == null || _currentSkill.VfxPrefab == null) return;
            var spawnedObj = ObjectPoolManager.Instance.Spawn(_currentSkill.VfxPrefab);
            if (!spawnedObj.TryGetComponent<Projectile>(out var projectile))
            {
                return;
            }

            projectile.transform.SetPositionAndRotation(_effectSpawnPoint.position, _effectSpawnPoint.rotation);
            projectile.Launch(_effectSpawnPoint.position, _currentSkill.GetCurrentTargetWorldPosition());
            projectile.OnReachTarget = OnApplyEffect;
        }

        /// <summary>
        /// Animation Event: Được gọi tại frame mà skill có hiệu ứng xuất hiện (vd: slash vfx, buff vfx)
        /// </summary>
        public void AnimEvent_StartEffect()
        {
            if (_currentSkill == null) return;
            if (_currentSkill.VfxPrefab != null)
            {
                var spawnedObj = ObjectPoolManager.Instance.Spawn(_currentSkill.VfxPrefab);
                spawnedObj.transform.SetPositionAndRotation(_effectSpawnPoint.position, _effectSpawnPoint.rotation);
            }

            if (_currentSkill.HasDelayApplyEffect)
            {
                StartCoroutine(DelayedEffect());
            }
            else
            {
                OnApplyEffect();
            }
        }

        private System.Collections.IEnumerator DelayedEffect()
        {
            yield return new WaitForSeconds(_currentSkill.DelayApplyEffectTime);
            OnApplyEffect();
        }

        /// <summary>
        /// Animation Event: Được gọi khi attack animation hoàn thành
        /// Thêm event này vào Attack Animation Clip tại frame cuối
        /// </summary>
        public void AnimEvent_AttackComplete()
        {
        }

        /// <summary>
        /// Animation Event: Được gọi khi death animation hoàn thành
        /// Thêm event này vào Death Animation Clip tại frame cuối
        /// </summary>
        public void AnimEvent_DeathComplete()
        {
        }

        #endregion
    }
}
