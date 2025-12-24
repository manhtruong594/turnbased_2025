using System;
using System.Collections.Generic;

namespace TurnBasedGame.ObjectPool
{
    /// <summary>
    /// Generic Object Pool - có thể pool bất kỳ type nào implement IPoolable
    /// Sử dụng Strategy Pattern cho việc tạo và destroy objects
    /// </summary>
    /// <typeparam name="T">Type cần pool (phải implement IPoolable)</typeparam>
    public class ObjectPool<T> where T : IPoolable
    {
        private readonly Stack<T> _availableObjects;
        private readonly HashSet<T> _activeObjects;
        private readonly Func<T> _createFunc;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly Action<T> _onDestroy;
        private readonly int _maxSize;
        private readonly bool _collectionCheck;

        public int CountActive => _activeObjects.Count;
        public int CountAvailable => _availableObjects.Count;
        public int CountAll => CountActive + CountAvailable;

        /// <summary>
        /// Constructor cho Object Pool
        /// </summary>
        /// <param name="createFunc">Func để tạo object mới</param>
        /// <param name="onGet">Action khi lấy object từ pool (optional)</param>
        /// <param name="onRelease">Action khi trả object về pool (optional)</param>
        /// <param name="onDestroy">Action khi destroy object (optional)</param>
        /// <param name="collectionCheck">Kiểm tra object đã release chưa trước khi release lần nữa</param>
        /// <param name="defaultCapacity">Số lượng objects khởi tạo ban đầu</param>
        /// <param name="maxSize">Số lượng objects tối đa trong pool (0 = unlimited)</param>
        public ObjectPool(
            Func<T> createFunc,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDestroy = null,
            bool collectionCheck = true,
            int defaultCapacity = 10,
            int maxSize = 100)
        {
            if (createFunc == null)
                throw new ArgumentNullException(nameof(createFunc));

            _createFunc = createFunc;
            _onGet = onGet;
            _onRelease = onRelease;
            _onDestroy = onDestroy;
            _collectionCheck = collectionCheck;
            _maxSize = maxSize;

            _availableObjects = new Stack<T>(defaultCapacity);
            _activeObjects = new HashSet<T>();
        }

        /// <summary>
        /// Lấy object từ pool hoặc tạo mới nếu pool rỗng
        /// </summary>
        public T Get()
        {
            T obj;
            if (_availableObjects.Count > 0)
            {
                obj = _availableObjects.Pop();
            }
            else
            {
                obj = _createFunc();
            }

            _activeObjects.Add(obj);
            _onGet?.Invoke(obj);
            obj.OnSpawnFromPool();

            return obj;
        }

        /// <summary>
        /// Trả object về pool
        /// </summary>
        public void Release(T obj)
        {
            if (_collectionCheck && !_activeObjects.Contains(obj))
                throw new InvalidOperationException("Attempting to release object that is not from this pool or already released.");

            _activeObjects.Remove(obj);
            obj.OnReturnToPool();
            _onRelease?.Invoke(obj);

            // Nếu pool đã đầy và có maxSize, destroy object
            if (_maxSize > 0 && _availableObjects.Count >= _maxSize)
            {
                _onDestroy?.Invoke(obj);
            }
            else
            {
                _availableObjects.Push(obj);
            }
        }

        /// <summary>
        /// Xóa tất cả objects trong pool
        /// </summary>
        public void Clear()
        {
            // Destroy các objects còn available
            while (_availableObjects.Count > 0)
            {
                var obj = _availableObjects.Pop();
                _onDestroy?.Invoke(obj);
            }

            // Destroy các objects đang active
            foreach (var obj in _activeObjects)
            {
                _onDestroy?.Invoke(obj);
            }

            _activeObjects.Clear();
        }

        /// <summary>
        /// Pre-warm pool với số lượng objects nhất định
        /// </summary>
        public void Prewarm(int count)
        {
            var tempList = new List<T>(count);
            for (int i = 0; i < count; i++)
            {
                tempList.Add(Get());
            }

            foreach (var obj in tempList)
            {
                Release(obj);
            }
        }
    }
}
