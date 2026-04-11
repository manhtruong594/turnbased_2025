using UnityEngine;
using System.Collections.Generic;

namespace TurnBasedGame.ObjectPool
{
    /// <summary>
    /// Singleton Manager quản lý tất cả pools trong game
    /// Sử dụng Facade Pattern để đơn giản hóa việc sử dụng pool system
    /// </summary>
    public class ObjectPoolManager : BaseManager
    {
        public static ObjectPoolManager Instance { get; private set; }

        private Dictionary<string, ObjectPool<PooledObject>> _pools = new Dictionary<string, ObjectPool<PooledObject>>();
        private Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Tạo pool mới cho prefab
        /// </summary>
        public void CreatePool(string key, GameObject prefab, int initialSize = 1, int maxSize = 100, bool prewarm = true)
        {
            if (_pools.ContainsKey(key))
            {
                Debug.LogWarning($"Pool with key '{key}' already exists.");
                return;
            }

            if (prefab == null)
            {
                Debug.LogError($"Prefab for pool '{key}' is null.");
                return;
            }

            _prefabCache[key] = prefab;

            var pool = new ObjectPool<PooledObject>(
                createFunc: () => CreatePooledObject(key),
                onGet: obj => obj.gameObject.SetActive(true),
                onRelease: obj => obj.gameObject.SetActive(false),
                onDestroy: obj => { if (obj != null && obj.gameObject != null) Destroy(obj.gameObject); },
                collectionCheck: true,
                defaultCapacity: initialSize,
                maxSize: maxSize
            );

            _pools[key] = pool;

            if (prewarm)
            {
                pool.Prewarm(initialSize);
            }

            Debug.Log($"Pool '{key}' created with initial size: {initialSize}, max size: {maxSize}");
        }

        private PooledObject CreatePooledObject(string key)
        {
            if (!_prefabCache.TryGetValue(key, out var prefab))
            {
                Debug.LogError($"Prefab for key '{key}' not found in cache.");
                return null;
            }

            var instance = Instantiate(prefab);
            instance.name = $"{prefab.name}_Pooled";

            var pooledObj = instance.GetComponent<PooledObject>();
            if (pooledObj == null)
            {
                pooledObj = instance.AddComponent<PooledObject>();
            }

            pooledObj.Initialize(key, this);
            return pooledObj;
        }

        /// <summary>
        /// Lấy object từ pool
        /// </summary>
        public GameObject Spawn(string key)
        {
            if (!_pools.TryGetValue(key, out var pool))
            {
                Debug.LogError($"Pool with key '{key}' does not exist. Creating pool at runtime.");
                if (_prefabCache.TryGetValue(key, out var prefab))
                {
                    CreatePool(key, prefab);
                    return Spawn(key);
                }
                return null;
            }

            var pooledObj = pool.Get();
            if (pooledObj == null)
                return null;

            var obj = pooledObj.gameObject;
            return obj;
        }

        /// <summary>
        /// Lấy object từ pool với parent
        /// </summary>
        public GameObject Spawn(string key, Transform parent)
        {
            var obj = Spawn(key);
            if (obj != null && parent != null)
            {
                obj.transform.SetParent(parent, false);
            }

            return obj;
        }
        
        public GameObject Spawn(GameObject prefab, Transform parent = null)
        {
            if (!_pools.TryGetValue(prefab.name, out var pool))
            {
                if (_prefabCache.TryGetValue(prefab.name, out var cachedPrefab))
                {
                    CreatePool(prefab.name, cachedPrefab);
                    return Spawn(prefab.name, parent);
                }
                else
                {
                    CreatePool(prefab.name, prefab);
                    return Spawn(prefab.name, parent);
                }
            }

            var pooledObj = pool.Get();
            if (pooledObj == null)
                return null;
            if (parent != null)
            {
                pooledObj.transform.SetParent(parent, false);
            }
            return pooledObj.gameObject;
        }   

        public T Spawn<T>(string key, Transform parent = null) where T : PooledObject
        {
            var obj = Spawn(key, parent);
            return obj != null ? obj.GetComponent<T>() : null;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var obj = Spawn(prefab);
            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
            }
            return obj;
        }
        
        /// <summary>
        /// Trả object về pool
        /// </summary>
        public void Despawn(string key, PooledObject obj)
        {
            if (obj == null)
                return;

            var go = obj.gameObject;
            if (go == null)
            {
                Debug.LogWarning($"Object {obj.name} is not pooled. Destroying instead.");
                Destroy(obj);
                return;
            }

            if (!_pools.TryGetValue(key, out var pool))
            {
                Debug.LogWarning($"Pool with key '{key}' does not exist.");
                return;
            }

            pool.Release(obj);
        }

        /// <summary>
        /// Trả object về pool tự động (pooledObj tự biết pool key của nó)
        /// </summary>
        public void Despawn(PooledObject pooledObj)
        {
            if (pooledObj == null)
                return;

            Despawn(pooledObj.PoolKey, pooledObj);
        }

        /// <summary>
        /// Xóa pool
        /// </summary>
        public void DestroyPool(string key)
        {
            if (_pools.TryGetValue(key, out var pool))
            {
                pool.Clear();
                _pools.Remove(key);
                _prefabCache.Remove(key);
                Debug.Log($"Pool '{key}' destroyed.");
            }
        }

        /// <summary>
        /// Xóa tất cả pools
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }
            _pools.Clear();
            _prefabCache.Clear();
        }

        /// <summary>
        /// Lấy thông tin về pool
        /// </summary>
        public (int active, int available, int total) GetPoolInfo(string key)
        {
            if (_pools.TryGetValue(key, out var pool))
            {
                return (pool.CountActive, pool.CountAvailable, pool.CountAll);
            }
            return (0, 0, 0);
        }

        void OnDestroy()
        {
            ClearAllPools();
        }

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        void OnGUI()
        {
            if (!showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 100, 300, 400));
            GUILayout.Label("=== Object Pool Debug ===");

            foreach (var kvp in _pools)
            {
                var info = GetPoolInfo(kvp.Key);
                GUILayout.Label($"{kvp.Key}: Active={info.active}, Available={info.available}");
            }

            GUILayout.EndArea();
        }
#endif
    }
}
