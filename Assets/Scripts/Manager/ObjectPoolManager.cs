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
        private Dictionary<GameObject, string> _poolKeyByPrefab = new Dictionary<GameObject, string>();

        [Header("Pool Settings")]
        [Tooltip("Số object tạo sẵn khi pool VFX được tạo lười (lazy). Giữ >= 3 để cast liên tiếp không gây Instantiate giữa combat.")]
        [SerializeField] private int vfxPoolInitialSize = 4;

        [Tooltip("Số object tối thiểu được giữ sẵn trong pool VFX sau khi prewarm.")]
        [SerializeField] private int vfxPrewarmSize = 4;

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

            // Ghi nhận key theo prefab để Spawn(GameObject) tái sử dụng pool đã đăng ký
            // thay vì tạo pool trùng cho cùng một prefab.
            if (!_poolKeyByPrefab.ContainsKey(prefab))
                _poolKeyByPrefab[prefab] = key;

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
            if (prefab == null) return null;

            var key = GetPoolKeyForPrefab(prefab);
            if (!_pools.TryGetValue(key, out var pool))
            {
                CreatePool(key, prefab, vfxPoolInitialSize);
                if (!_pools.TryGetValue(key, out pool))
                    return null;
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

        /// <summary>
        /// Lấy key pool duy nhất cho prefab. Các prefab trùng tên (ví dụ nhiều VFX_Buff_Cast,
        /// Shield_gold) phải dùng pool riêng nên key không được chỉ là prefab.name.
        /// </summary>
        private string GetPoolKeyForPrefab(GameObject prefab)
        {
            if (_poolKeyByPrefab.TryGetValue(prefab, out var existing))
                return existing;

            var key = $"{prefab.name}#{prefab.GetInstanceID()}";
            _poolKeyByPrefab[prefab] = key;
            return key;
        }

        /// <summary>
        /// Đảm bảo pool của prefab có sẵn ít nhất size object, tránh Instantiate giữa combat.
        /// </summary>
        public void PrewarmVfx(GameObject prefab, int count = 0)
        {
            if (prefab == null) return;

            var size = count > 0 ? count : vfxPrewarmSize;
            var key = GetPoolKeyForPrefab(prefab);
            if (!_pools.TryGetValue(key, out var pool))
            {
                CreatePool(key, prefab, size);
                return;
            }

            var missing = size - pool.CountAll;
            for (int i = 0; i < missing; i++)
            {
                pool.Prewarm(1);
            }
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
                if (_prefabCache.Remove(key, out var prefab))
                    ForgetPrefabKey(prefab);
                Debug.Log($"Pool '{key}' destroyed.");
            }
        }

        private void ClearPrefabKeyCache()
        {
            _poolKeyByPrefab.Clear();
        }

        private void ForgetPrefabKey(GameObject prefab)
        {
            if (prefab != null)
                _poolKeyByPrefab.Remove(prefab);
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
            ClearPrefabKeyCache();
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

    }
}
