namespace TurnBasedGame.ObjectPool
{
    /// <summary>
    /// Interface cho objects có thể được pool
    /// Các object muốn được pool phải implement interface này
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Được gọi khi object được lấy ra từ pool
        /// </summary>
        void OnSpawnFromPool();

        /// <summary>
        /// Được gọi khi object được trả về pool
        /// </summary>
        void OnReturnToPool();

        /// <summary>
        /// Kiểm tra object có đang active không
        /// </summary>
        bool IsActive { get; }
    }
}
