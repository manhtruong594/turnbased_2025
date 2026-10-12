using UnityEngine;

namespace TurnBasedGame.ObjectPool
{
    /// <summary>
    /// Component cho VFX pooling - tự động despawn khi mọi particle trong hierarchy kết thúc
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class PooledVFX : MonoBehaviour, IPoolable
    {
        private ParticleSystem _particleSystem;
        private PooledObject _pooledObject;
        private bool _isPlaying;
        private ParticleSystem[] _allParticleSystems;

        public bool IsActive => _isPlaying;

        void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            _pooledObject = GetComponent<PooledObject>();
            CacheParticleSystems();
        }

        void OnEnable()
        {
            CacheParticleSystems();
            OnSpawnFromPool();
        }

        void OnDisable()
        {
            OnReturnToPool();
        }

        void Update()
        {
            if (!_isPlaying) return;
            if (_allParticleSystems == null) CacheParticleSystems();

            for (int i = 0; i < _allParticleSystems.Length; i++)
            {
                if (_allParticleSystems[i] != null && _allParticleSystems[i].IsAlive(true))
                    return;
            }

            if (_pooledObject == null)
                TryGetComponent(out _pooledObject);

            _pooledObject?.Despawn();
        }

        private void CacheParticleSystems()
        {
            _allParticleSystems = GetComponentsInChildren<ParticleSystem>(true);
        }

        public void OnSpawnFromPool()
        {
            _isPlaying = true;
            if (_particleSystem != null)
                _particleSystem.Play(true);
        }

        public void OnReturnToPool()
        {
            _isPlaying = false;
            if (_particleSystem != null)
                _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
