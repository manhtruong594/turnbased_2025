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

        // Instance pool được tái sử dụng: chỉ cấu hình auto-despawn một lần.
        private static readonly System.Collections.Generic.HashSet<int> PreparedVfxInstances = new();

        private Transform _casterRoot;
        [SerializeField] private Transform _effectSpawnPoint;
        [SerializeField] private Vector3 _casterOffset = new Vector3(0, 1.5f, 0);

        void Awake()
        {
            _casterRoot = transform;
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
            var casterUnit = GetComponentInParent<UnitController>() ?? GetComponent<UnitController>();
            Vector3 targetWorld = skill.GetVisualTargetWorldPosition(casterUnit);
            Vector3 toTarget = targetWorld - startPosition;
            toTarget.y = 0f;
            Quaternion initialRotation = toTarget.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(toTarget.normalized, Vector3.up)
                : GetCasterFacingRotation();

            projectile.transform.SetPositionAndRotation(startPosition, initialRotation);
            projectile.OnReachTarget = () =>
            {
                SpawnImpactVfx(skill);

                if (timing == SkillEffectApplyTiming.OnProjectileImpact)
                    skill.ApplyEffect();
            };
            projectile.Launch(startPosition, targetWorld);
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
            if (!PreparedVfxInstances.Add(instance.GetInstanceID())) return;

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

            return _casterRoot;
        }

        private Quaternion GetCasterFacingRotation()
        {
            var unit = GetComponentInParent<UnitController>() ?? GetComponent<UnitController>();
            if (unit != null && unit.RotationNode != null)
            {
                Vector3 forward = unit.RotationNode.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.001f)
                    return Quaternion.LookRotation(forward.normalized, Vector3.up);
            }

            Vector3 rootFwd = _casterRoot != null ? _casterRoot.forward : Vector3.forward;
            rootFwd.y = 0f;
            return rootFwd.sqrMagnitude > 0.001f ? Quaternion.LookRotation(rootFwd.normalized, Vector3.up) : Quaternion.identity;
        }

        private SpawnPoint ResolveSpawnPoint(SkillVfxSpawnPoint spawnPointType, SkillBase skill)
        {
            Quaternion casterRot = GetCasterFacingRotation();

            return spawnPointType switch
            {
                SkillVfxSpawnPoint.Caster => new SpawnPoint(GetCasterPosition(), casterRot),
                SkillVfxSpawnPoint.Target => new SpawnPoint(skill.GetCurrentTargetWorldPosition(), casterRot),
                _ => new SpawnPoint(GetEffectSpawnPoint().position, casterRot)
            };
        }

        private Vector3 GetCasterPosition()
        {
            return _casterRoot.position + _casterOffset;
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
