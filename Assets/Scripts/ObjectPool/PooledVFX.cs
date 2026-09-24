using UnityEngine;

namespace TurnBasedGame.ObjectPool
{
    /// <summary>
    /// Component cho VFX pooling - tự động despawn khi particle hết
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class PooledVFX : MonoBehaviour, IPoolable
    {
        private ParticleSystem _particleSystem;
        private PooledObject _pooledObject;
        private bool _isPlaying;

        public bool IsActive => _isPlaying;

        void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            _pooledObject = GetComponent<PooledObject>();
        }

        void OnEnable()
        {
            OnSpawnFromPool();
        }

        void OnDisable()
        {
            OnReturnToPool();
        }

        void Update()
        {
            if (_isPlaying && !_particleSystem.IsAlive(true))
            {
                if (_pooledObject == null)
                    TryGetComponent(out _pooledObject);

                _pooledObject?.Despawn();
            }
        }

        public void OnSpawnFromPool()
        {
            _isPlaying = true;
            _particleSystem.Play(true);
        }

        public void OnReturnToPool()
        {
            _isPlaying = false;
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
