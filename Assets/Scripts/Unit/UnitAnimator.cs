using System;
using TurnBasedGame.Skills;
using UnityEngine;

namespace TurnBasedGame.Unit
{
    /// <summary>
    /// Controls unit animations and forwards animation events as skill cues.
    /// </summary>
    public class UnitAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;

        [Header("Animation Settings")]
        [SerializeField] private float defaultAttackSpeed = 1f;
        [SerializeField] private Transform _effectSpawnPoint;
        [SerializeField] private float _transitionDuration = 0.1f;
        [SerializeField] private SkillEffectRunner _skillEffectRunner;

        private SkillBase _currentSkill;

        private void Awake()
        {
            if (_skillEffectRunner == null && !TryGetComponent(out _skillEffectRunner))
            {
                _skillEffectRunner = gameObject.AddComponent<SkillEffectRunner>();
            }

            _skillEffectRunner.Initialize(transform, _effectSpawnPoint);
        }

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

            _currentSkill = skill;
            animator.CrossFadeInFixedTime(AnimationHashLib.GetHashAnimByAttackType(skill.Type), _transitionDuration);
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

        #region Animation Events

        public void AnimEvent_SkillCue(string cueName)
        {
            if (!Enum.TryParse(cueName, true, out SkillAnimationCue cue))
            {
                Debug.LogWarning($"Invalid skill animation cue: {cueName}");
                return;
            }

            _skillEffectRunner.HandleCue(_currentSkill, cue);
        }

        public void AnimEvent_Cast()
        {
            _skillEffectRunner.HandleCue(_currentSkill, SkillAnimationCue.Cast);
        }

        public void AnimEvent_Release()
        {
            _skillEffectRunner.HandleCue(_currentSkill, SkillAnimationCue.Release);
        }

        public void AnimEvent_Impact()
        {
            _skillEffectRunner.HandleCue(_currentSkill, SkillAnimationCue.Impact);
        }

        public void AnimEvent_AttackHit()
        {
            AnimEvent_Impact();
        }

        public void AnimEvent_AttackStart()
        {
            AnimEvent_Release();
        }

        public void AnimEvent_StartEffect()
        {
            AnimEvent_Impact();
        }

        public void AnimEvent_AttackComplete()
        {
            _skillEffectRunner.HandleCue(_currentSkill, SkillAnimationCue.Complete);
        }

        public void AnimEvent_DeathComplete()
        {
        }

        #endregion
    }
}
