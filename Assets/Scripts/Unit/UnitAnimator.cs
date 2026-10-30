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
        [SerializeField] private float _transitionDuration = 0.1f;
        [SerializeField] private SkillEffectRunner _skillEffectRunner;


        private SkillBase _currentSkill;

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

        private Coroutine _safetyFlushCoroutine;

        public void PlayAttack(SkillBase skill)
        {
            if (animator == null) return;

            _currentSkill = skill;
            animator.CrossFadeInFixedTime(AnimationHashLib.GetHashAnimByAttackType(skill.Type), _transitionDuration);

            if (_safetyFlushCoroutine != null) StopCoroutine(_safetyFlushCoroutine);
            _safetyFlushCoroutine = StartCoroutine(SafetyFlushTimer(skill, 2.5f));
        }

        private System.Collections.IEnumerator SafetyFlushTimer(SkillBase skill, float delay)
        {
            yield return new WaitForSeconds(delay);
            skill?.FlushRemainingVisualHits();
            _safetyFlushCoroutine = null;
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

        public void PlayStunned()
        {
            if (animator == null) return;
            animator.CrossFadeInFixedTime(AnimationHashLib.Stunned, _transitionDuration);
        }

        public void PreviewAnimation(int stateHash)
        {
            if (animator == null) return;
            _currentSkill = null;
            animator.SetBool(AnimationHashLib.IsMoving, false);
            animator.CrossFadeInFixedTime(stateHash, _transitionDuration);
        }

        #endregion

        #region Animation Events

        public Animator Animator => animator;

        /// <summary>Raised for every animation event received; used by the skill test scene.</summary>
        public event Action<string> AnimationEventRaised;

        public void AnimEvent_SkillCue(string cueName)
        {
            LogAnimationEvent("SkillCue(" + cueName + ")");
            if (!Enum.TryParse(cueName, true, out SkillAnimationCue cue))
            {
                Debug.LogWarning($"Invalid skill animation cue: {cueName}");
                return;
            }

            _skillEffectRunner.HandleCue(_currentSkill, cue);
        }
        
        public void AnimEvent_AttackComplete()
        {
            LogAnimationEvent("AttackComplete");
            _skillEffectRunner.HandleCue(_currentSkill, SkillAnimationCue.Complete);
            if (_safetyFlushCoroutine != null)
            {
                StopCoroutine(_safetyFlushCoroutine);
                _safetyFlushCoroutine = null;
            }
            _currentSkill?.FlushRemainingVisualHits();
        }

        public void AnimEvent_DeathComplete()
        {
            AnimationEventRaised?.Invoke("DeathComplete");
        }

        private void LogAnimationEvent(string eventName)
        {
            #if UNITY_EDITOR
            AnimationEventRaised?.Invoke(eventName);
            #endif
        }

        #endregion
    }
}
