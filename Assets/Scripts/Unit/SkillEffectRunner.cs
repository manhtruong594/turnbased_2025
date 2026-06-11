using System.Collections;
using TurnBasedGame.ObjectPool;
using TurnBasedGame.Skills;
using TurnBasedGame.VFX;
using UnityEngine;

namespace TurnBasedGame.Unit
{
    public class SkillEffectRunner : MonoBehaviour
    {
        private Transform _casterRoot;
        private Transform _effectSpawnPoint;

        public void Initialize(Transform casterRoot, Transform effectSpawnPoint)
        {
            _casterRoot = casterRoot;
            _effectSpawnPoint = effectSpawnPoint;
        }

        public void HandleCue(SkillBase skill, SkillAnimationCue cue)
        {
            if (skill == null) return;

            switch (cue)
            {
                case SkillAnimationCue.Cast:
                    SpawnVfx(skill.GetCastVfxPrefab(), skill.VfxConfig.CastSpawnPoint, skill);
                    break;
                case SkillAnimationCue.Release:
                    HandleReleaseCue(skill);
                    break;
                case SkillAnimationCue.Impact:
                    HandleImpactCue(skill);
                    break;
                case SkillAnimationCue.Complete:
                    break;
            }
        }

        private void HandleReleaseCue(SkillBase skill)
        {
            var releaseVfxPrefab = skill.GetReleaseVfxPrefab();
            var projectilePrefab = ResolveProjectilePrefab(skill, releaseVfxPrefab);
            bool hasProjectile = projectilePrefab != null;
            var timing = skill.ResolveEffectApplyTiming(hasProjectile);

            if (releaseVfxPrefab != null && releaseVfxPrefab != projectilePrefab)
                SpawnVfx(releaseVfxPrefab, skill.VfxConfig.ReleaseSpawnPoint, skill);

            if (hasProjectile)
                LaunchProjectile(skill, projectilePrefab, timing);
            else if (releaseVfxPrefab != null)
                SpawnVfx(releaseVfxPrefab, skill.VfxConfig.ReleaseSpawnPoint, skill);

            if (timing == SkillEffectApplyTiming.OnRelease)
                skill.ApplyEffect();
        }

        private void HandleImpactCue(SkillBase skill)
        {
            bool hasProjectile = ResolveProjectilePrefab(skill, skill.GetReleaseVfxPrefab()) != null;
            var timing = skill.ResolveEffectApplyTiming(hasProjectile);

            if (timing == SkillEffectApplyTiming.OnProjectileImpact)
                return;

            SpawnImpactVfx(skill);

            if (timing == SkillEffectApplyTiming.OnAnimationImpact)
                skill.ApplyEffect();
        }

        private GameObject ResolveProjectilePrefab(SkillBase skill, GameObject releaseVfxPrefab)
        {
            if (skill.GetProjectilePrefab() != null)
                return skill.GetProjectilePrefab();

            if (releaseVfxPrefab != null && releaseVfxPrefab.TryGetComponent<Projectile>(out _))
                return releaseVfxPrefab;

            return null;
        }

        private void LaunchProjectile(SkillBase skill, GameObject projectilePrefab, SkillEffectApplyTiming timing)
        {
            var spawnPoint = GetEffectSpawnPoint();
            var spawnedObj = ObjectPoolManager.Instance.Spawn(projectilePrefab);
            if (spawnedObj == null || !spawnedObj.TryGetComponent<Projectile>(out var projectile))
                return;

            var startPosition = spawnPoint.position;
            projectile.transform.SetPositionAndRotation(startPosition, spawnPoint.rotation);
            projectile.OnReachTarget = () =>
            {
                SpawnImpactVfx(skill);

                if (timing == SkillEffectApplyTiming.OnProjectileImpact)
                    skill.ApplyEffect();
            };
            projectile.Launch(startPosition, skill.GetCurrentTargetWorldPosition());
        }

        private void SpawnImpactVfx(SkillBase skill)
        {
            SpawnVfx(skill.GetImpactVfxPrefab(), skill.VfxConfig.ImpactSpawnPoint, skill);
        }

        private void SpawnVfx(GameObject prefab, SkillVfxSpawnPoint spawnPointType, SkillBase skill)
        {
            if (prefab == null) return;

            var spawnPoint = ResolveSpawnPoint(spawnPointType, skill);
            ObjectPoolManager.Instance.Spawn(prefab, spawnPoint.Position, spawnPoint.Rotation);
        }

        private Transform GetEffectSpawnPoint()
        {
            if (_effectSpawnPoint != null)
                return _effectSpawnPoint;

            return _casterRoot != null ? _casterRoot : transform;
        }

        private SpawnPoint ResolveSpawnPoint(SkillVfxSpawnPoint spawnPointType, SkillBase skill)
        {
            return spawnPointType switch
            {
                SkillVfxSpawnPoint.Caster => new SpawnPoint(GetCasterPosition(), GetCasterRotation()),
                SkillVfxSpawnPoint.Target => new SpawnPoint(skill.GetCurrentTargetWorldPosition(), Quaternion.identity),
                _ => new SpawnPoint(GetEffectSpawnPoint().position, GetEffectSpawnPoint().rotation)
            };
        }

        private Vector3 GetCasterPosition()
        {
            return _casterRoot != null ? _casterRoot.position : transform.position;
        }

        private Quaternion GetCasterRotation()
        {
            return _casterRoot != null ? _casterRoot.rotation : transform.rotation;
        }

        private readonly struct SpawnPoint
        {
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;

            public SpawnPoint(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
            }
        }
    }
}
