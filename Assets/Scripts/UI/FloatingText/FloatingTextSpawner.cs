using UnityEngine;
using TurnBasedGame.ObjectPool;

namespace TurnBasedGame.UI
{
    /// <summary>
    /// Spawn FloatingText instances trên đầu unit để hiển thị damage/heal.
    /// Tích hợp ObjectPoolManager để tái sử dụng.
    /// </summary>
    public class FloatingTextSpawner : MonoBehaviour
    {
        public static FloatingTextSpawner Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private GameObject floatingTextPrefab;
        [SerializeField] private int poolInitialSize = 5;
        [SerializeField] private int poolMaxSize = 20;

        [Header("Spawn Offset")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2f, 0f);

        private const string POOL_KEY = "FloatingText";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (ObjectPoolManager.Instance != null && floatingTextPrefab != null)
                ObjectPoolManager.Instance.CreatePool(POOL_KEY, floatingTextPrefab, poolInitialSize, poolMaxSize, true);
        }

        /// <summary>
        /// Hiển thị số damage bay theo hình parabol.
        /// </summary>
        public void SpawnDamage(Vector3 worldPosition, int amount)
        {
            Spawn(worldPosition, amount, true);
        }

        /// <summary>
        /// Hiển thị số heal bay thẳng lên trên.
        /// </summary>
        public void SpawnHeal(Vector3 worldPosition, int amount)
        {
            Spawn(worldPosition, amount, false);
        }

        private void Spawn(Vector3 worldPosition, int amount, bool isDamage)
        {
            if (amount <= 0) return;

            GameObject obj = null;

            if (ObjectPoolManager.Instance != null)
                obj = ObjectPoolManager.Instance.Spawn(POOL_KEY);

            if (obj == null && floatingTextPrefab != null)
                obj = Instantiate(floatingTextPrefab);

            if (obj == null)
            {
                Debug.LogWarning("[FloatingTextSpawner] Cannot spawn FloatingText.");
                return;
            }

            obj.transform.SetParent(transform, false);
            obj.transform.position = worldPosition + worldOffset;
            obj.SetActive(true);
            var floatingText = obj.GetComponent<FloatingText>();
            if (floatingText != null)
                floatingText.Setup(amount, isDamage);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
