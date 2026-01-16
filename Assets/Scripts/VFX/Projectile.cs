using UnityEngine;
using System;
using TurnBasedGame.ObjectPool;

namespace TurnBasedGame.VFX
{
    /// <summary>
    /// Component quản lý chuyển động của projectile
    /// Hỗ trợ 2 loại đường bay: thẳng và cầu vồng (parabol)
    /// </summary>
    public class Projectile : MonoBehaviour, IPoolable
    {
        public enum TrajectoryType
        {
            Linear,     // Bay thẳng
            Arc         // Bay cầu vồng (parabol)
        }

        [Header("Settings")]
        [SerializeField] private TrajectoryType trajectoryType = TrajectoryType.Linear;
        [SerializeField] private float speed = 10f;
        [SerializeField] private float arcHeight = 3f; // Độ cao cầu vồng (chỉ dùng cho Arc)
        [SerializeField] private bool rotateToDirection = true;

        [Header("VFX")]
        [SerializeField] private ParticleSystem trailEffect;
        [SerializeField] private GameObject hitEffectPrefab;

        // Private state
        private Vector3 _startPosition;
        private Vector3 _targetPosition;
        private float _progress;
        private float _duration;
        private bool _isFlying;
        private PooledObject _pooledObject;

        // Events
        public Action<Vector3> OnReachTarget;

        public bool IsActive => _isFlying;

        void Awake()
        {
            _pooledObject = GetComponent<PooledObject>();
        }

        void Update()
        {
            if (!_isFlying) return;

            _progress += Time.deltaTime / _duration;

            if (_progress >= 1f)
            {
                ReachTarget();
                return;
            }

            UpdatePosition();
            UpdateRotation();
        }

        #region Public Methods

        /// <summary>
        /// Khởi động projectile với tham số tùy chỉnh
        /// </summary>
        public void Launch(Vector3 start, Vector3 target, TrajectoryType type, float customSpeed = -1f, float customArcHeight = -1f)
        {
            _startPosition = start;
            _targetPosition = target;
            trajectoryType = type;
            
            if (customSpeed > 0) speed = customSpeed;
            if (customArcHeight > 0) arcHeight = customArcHeight;

            transform.position = start;
            _progress = 0f;
            _duration = CalculateDuration();
            _isFlying = true;

            if (trailEffect != null)
                trailEffect.Play();
        }

        /// <summary>
        /// Khởi động projectile với settings mặc định
        /// </summary>
        public void Launch(Vector3 start, Vector3 target)
        {
            Launch(start, target, trajectoryType, speed, arcHeight);
        }

        #endregion

        #region IPoolable Implementation

        public void OnSpawnFromPool()
        {
            // Reset state khi spawn
            _isFlying = false;
            _progress = 0f;
        }

        public void OnReturnToPool()
        {
            // Cleanup khi despawn
            _isFlying = false;
            OnReachTarget = null;

            if (trailEffect != null)
                trailEffect.Stop();
        }

        #endregion

        #region Private Methods

        private float CalculateDuration()
        {
            float distance = Vector3.Distance(_startPosition, _targetPosition);
            return distance / speed;
        }

        private void UpdatePosition()
        {
            Vector3 position = trajectoryType switch
            {
                TrajectoryType.Linear => CalculateLinearPosition(),
                TrajectoryType.Arc => CalculateArcPosition(),
                _ => CalculateLinearPosition()
            };

            transform.position = position;
        }

        private Vector3 CalculateLinearPosition()
        {
            return Vector3.Lerp(_startPosition, _targetPosition, _progress);
        }

        private Vector3 CalculateArcPosition()
        {
            // Lerp vị trí XZ
            Vector3 basePosition = Vector3.Lerp(_startPosition, _targetPosition, _progress);

            // Thêm độ cao parabol (công thức: h = 4 * height * t * (1 - t))
            float height = arcHeight * 4f * _progress * (1f - _progress);
            basePosition.y += height;

            return basePosition;
        }

        private void UpdateRotation()
        {
            if (!rotateToDirection) return;

            Vector3 direction = trajectoryType switch
            {
                TrajectoryType.Linear => (_targetPosition - _startPosition).normalized,
                TrajectoryType.Arc => CalculateArcDirection(),
                _ => Vector3.forward
            };

            if (direction != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(direction);
        }

        private Vector3 CalculateArcDirection()
        {
            // Tính hướng tiếp tuyến tại điểm hiện tại trên đường cong
            float nextProgress = Mathf.Min(_progress + 0.01f, 1f);
            Vector3 currentPos = CalculateArcPosition();
            
            Vector3 nextBasePos = Vector3.Lerp(_startPosition, _targetPosition, nextProgress);
            float nextHeight = arcHeight * 4f * nextProgress * (1f - nextProgress);
            nextBasePos.y += nextHeight;

            return (nextBasePos - currentPos).normalized;
        }

        private void ReachTarget()
        {
            _isFlying = false;
            transform.position = _targetPosition;

            OnReachTarget?.Invoke(_targetPosition);

            SpawnHitEffect();
            
            // Despawn sau khi hit
            _pooledObject?.DespawnAfter(0.1f);
        }

        private void SpawnHitEffect()
        {
            if (hitEffectPrefab == null) return;

            var effect = ObjectPoolManager.Instance?.Spawn(hitEffectPrefab)?.GetComponent<PooledVFX>();
            if (effect == null)
            {
                // Fallback nếu không dùng pool
                Instantiate(hitEffectPrefab, _targetPosition, Quaternion.identity);
            }
        }

        #endregion

        #region Editor Helpers

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            if (!_isFlying) return;

            // Vẽ đường bay trong Scene view
            Gizmos.color = Color.yellow;
            Vector3 prevPos = _startPosition;

            for (float t = 0f; t <= 1f; t += 0.05f)
            {
                Vector3 pos = trajectoryType switch
                {
                    TrajectoryType.Linear => Vector3.Lerp(_startPosition, _targetPosition, t),
                    TrajectoryType.Arc => CalculateArcPositionAtTime(t),
                    _ => Vector3.zero
                };

                Gizmos.DrawLine(prevPos, pos);
                prevPos = pos;
            }

            // Vẽ start và target
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_startPosition, 0.1f);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_targetPosition, 0.1f);
        }

        private Vector3 CalculateArcPositionAtTime(float t)
        {
            Vector3 basePos = Vector3.Lerp(_startPosition, _targetPosition, t);
            float height = arcHeight * 4f * t * (1f - t);
            basePos.y += height;
            return basePos;
        }

        #endregion
    }
}
