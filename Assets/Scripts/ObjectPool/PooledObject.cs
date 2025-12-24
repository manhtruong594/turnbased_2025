using UnityEngine;

namespace TurnBasedGame.ObjectPool
{
    /// <summary>
    /// MonoBehaviour wrapper cho pooled GameObjects
    /// Tự động xử lý lifecycle của pooled objects
    /// </summary>
    public class PooledObject : MonoBehaviour, IPoolable
    {
        public string PoolKey { get; private set; }
        public bool IsActive => gameObject.activeInHierarchy;

        private ObjectPoolManager _manager;
        /// <summary>
        /// Khởi tạo pooled object
        /// </summary>
        public void Initialize(string key, ObjectPoolManager manager)
        {
            PoolKey = key;
            _manager = manager;
        }

        /// <summary>
        /// Tự động despawn sau delay (seconds)
        /// </summary>
        public void DespawnAfter(float delay)
        {
            if (delay <= 0)
            {
                Despawn();
                return;
            }

            CancelInvoke(nameof(Despawn));
            Invoke(nameof(Despawn), delay);
        }

        /// <summary>
        /// Trả object về pool
        /// </summary>
        public void Despawn()
        {
            if (_manager != null)
            {
                _manager.Despawn(this);
            }
            else
            {
                Debug.LogWarning($"Manager is null for {gameObject.name}. Destroying instead.");
                Destroy(gameObject);
            }
        }

        #region IPoolable Implementation

        public void OnSpawnFromPool()
        {
        }

        public void OnReturnToPool()
        {
            CancelInvoke();
        }

        #endregion

        void OnDisable()
        {
            CancelInvoke();
        }
    }
}
