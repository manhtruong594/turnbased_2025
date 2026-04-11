using System;
using System.Collections.Generic;
using UnityEngine;
using TurnBasedGame.ObjectPool;

namespace TurnBasedGame.SpellCard
{
    /// <summary>
    /// Singleton quản lý spawn VFX theo StatusEffectType.
    /// Tự đăng ký pool với ObjectPoolManager khi Awake.
    /// </summary>
    public class EffectManager : MonoBehaviour
    {
        public static EffectManager Instance { get; private set; }

        [SerializeField] private List<Entry> _entries = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            RegisterPools();
        }

        /// <summary>Spawn VFX tại world position.</summary>
        public void SpawnEffect(StatusEffectType type, Vector3 worldPosition)
        {
            if (type == StatusEffectType.None) return;

            var obj = ObjectPoolManager.Instance?.Spawn(GetKey(type));
            if (obj == null) return;

            obj.transform.position = worldPosition;
        }

        public BuffEffect GetBuffEffect(StatusEffectType type)
        {
            if (type == StatusEffectType.None) return null;
            var src = _entries.Find(e => e.type == type);
            if (src == null)
            {
                Debug.LogWarning($"[EffectManager] No entry found for effect type {type}");
                return null;
            }
            return src.vfxPrefab;
        }
         
        private void RegisterPools()
        {
            var poolManager = ObjectPoolManager.Instance;
            if (poolManager == null)
            {
                Debug.LogWarning("[EffectManager] ObjectPoolManager not found. VFX pools not registered.");
                return;
            }

            foreach (var entry in _entries)
            {
                if (entry.vfxPrefab == null) continue;
                poolManager.CreatePool(GetKey(entry.type), entry.vfxPrefab.gameObject, 2);
            }
        }

        private static string GetKey(StatusEffectType type) =>type.ToString();

        [Serializable]
        public class Entry
        {
            public StatusEffectType type;
            public BuffEffect vfxPrefab;
        }
    }
}
