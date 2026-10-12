using System.Collections;
using TurnBasedGame.ObjectPool;
using TurnBasedGame.Skills;
using TurnBasedGame.VFX;
using UnityEngine;

namespace TurnBasedGame.Unit
{
    public class SkillEffectRunner : MonoBehaviour
    {
        private const float PooledVfxFallbackLifetime = 3f;

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
            var instance = ObjectPoolManager.Instance.Spawn(prefab, spawnPoint.Position, spawnPoint.Rotation);
            if (instance == null) return;

            EnsureAutoDespawn(instance);
        }

        /// <summary>
        /// Đảm bảo VFX được trả về pool khi particle kết thúc.
        /// Nhiều prefab VFX thiếu PooledVFX nên object không bao giờ despawn,
        /// pool luôn rỗng và mỗi lần cast phải Instantiate lại.
        /// Projectile có tự despawn (Projectile.cs) nên bỏ qua.
        /// </summary>
        private static void EnsureAutoDespawn(GameObject instance)
        {
            if (instance.GetComponent<PooledVFX>() != null) return;

            if (instance.TryGetComponent<Projectile>(out _)) return;

            if (!instance.TryGetComponent<ParticleSystem>(out _)) return;

            instance.AddComponent<PooledVFX>();

            // Prefab chỉ có particle loop thì PooledVFX không bao giờ despawn,
            // fallback bằng timer để object vẫn được trả về pool.
            if (instance.TryGetComponent<PooledObject>(out var pooledObject) &&
                !HasFiniteParticleSystem(instance))
            {
                pooledObject.DespawnAfter(PooledVfxFallbackLifetime);
            }
        }

        private static bool HasFiniteParticleSystem(GameObject instance)
        {
            var systems = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != null && !systems[i].main.loop)
                    return true;
            }

            return false;
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
