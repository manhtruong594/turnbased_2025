using UnityEngine;

namespace TurnBasedGame.ObjectPool.Examples
{
    /// <summary>
    /// Ví dụ sử dụng ObjectPoolManager
    /// Demo cách spawn và despawn objects từ pool
    /// </summary>
    public class ObjectPoolUsageExample : MonoBehaviour
    {
        [Header("Pool Keys")]
        [SerializeField] private string vfxPoolKey = "FireballVFX";
        [SerializeField] private string projectilePoolKey = "Projectile";

        [Header("Test Settings")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private KeyCode spawnVFXKey = KeyCode.V;

        void Update()
        {
            // Test spawn VFX
            if (Input.GetKeyDown(spawnVFXKey))
            {
                SpawnVFX();
            }
        }

        private void SpawnVFX()
        {
            if (ObjectPoolManager.Instance == null)
            {
                Debug.LogError("ObjectPoolManager not found!");
                return;
            }

            var obj = ObjectPoolManager.Instance.Spawn(vfxPoolKey);

            if (obj != null)
            {
                Debug.Log($"Spawned VFX: {obj.name}");
            }
        }

        /// <summary>
        /// Ví dụ tạo pool lúc runtime
        /// </summary>
        public void CreatePoolAtRuntime(string key, GameObject prefab, int initialSize = 10, int maxSize = 50)
        {
            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.CreatePool(key, prefab, initialSize, maxSize, true);
            }
        }

        /// <summary>
        /// Ví dụ spawn nhiều objects cùng lúc
        /// </summary>
        public void SpawnMultiple(string poolKey, int count, float radius)
        {
            for (int i = 0; i < count; i++)
            {
                var angle = i * (360f / count) * Mathf.Deg2Rad;
                var offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                ObjectPoolManager.Instance?.Spawn(poolKey);
            }
        }
    }
}
