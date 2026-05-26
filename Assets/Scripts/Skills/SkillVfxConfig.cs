using System;
using UnityEngine;

namespace TurnBasedGame.Skills
{
    public enum SkillAnimationCue
    {
        Cast,
        Release,
        Impact,
        Complete
    }

    public enum SkillEffectApplyTiming
    {
        Automatic,
        OnAnimationImpact,
        OnProjectileImpact,
        OnRelease
    }

    public enum SkillVfxSpawnPoint
    {
        Caster,
        EffectSpawnPoint,
        Target
    }

    [Serializable]
    public class SkillVfxConfig
    {
        [SerializeField] private GameObject castVfxPrefab;
        [SerializeField] private GameObject releaseVfxPrefab;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject impactVfxPrefab;
        [SerializeField] private SkillEffectApplyTiming effectApplyTiming = SkillEffectApplyTiming.Automatic;
        [SerializeField] private SkillVfxSpawnPoint castSpawnPoint = SkillVfxSpawnPoint.EffectSpawnPoint;
        [SerializeField] private SkillVfxSpawnPoint releaseSpawnPoint = SkillVfxSpawnPoint.EffectSpawnPoint;
        [SerializeField] private SkillVfxSpawnPoint impactSpawnPoint = SkillVfxSpawnPoint.Target;

        public GameObject CastVfxPrefab => castVfxPrefab;
        public GameObject ReleaseVfxPrefab => releaseVfxPrefab;
        public GameObject ProjectilePrefab => projectilePrefab;
        public GameObject ImpactVfxPrefab => impactVfxPrefab;
        public SkillEffectApplyTiming EffectApplyTiming => effectApplyTiming;
        public SkillVfxSpawnPoint CastSpawnPoint => castSpawnPoint;
        public SkillVfxSpawnPoint ReleaseSpawnPoint => releaseSpawnPoint;
        public SkillVfxSpawnPoint ImpactSpawnPoint => impactSpawnPoint;
    }
}
