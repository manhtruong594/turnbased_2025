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
        private float _despawnTime;
        private bool _isPlaying;

        public bool IsActive => _isPlaying;

        void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            _pooledObject = GetComponent<PooledObject>();
        }

        void Update()
        {
            if (_isPlaying && !_particleSystem.IsAlive())
            {
                _pooledObject?.Despawn();
            }
        }

        public void OnSpawnFromPool()
        {
            _isPlaying = true;
            _particleSystem.Play(true);

            // Backup: despawn after main duration nếu particle system có issue
            if (_particleSystem.main.duration > 0)
            {
                _despawnTime = _particleSystem.main.duration + _particleSystem.main.startLifetime.constantMax;
                _pooledObject?.DespawnAfter(_despawnTime);
            }
        }

        public void OnReturnToPool()
        {
            _isPlaying = false;
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
