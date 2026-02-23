using UnityEngine;
using TurnBasedGame.Core;

namespace TurnBasedGame.Unit
{
    /// <summary>
    /// Đánh dấu vị trí spawn hợp lệ trên map
    /// Mỗi spawn point thuộc về một người chơi cụ thể
    /// Thường đặt gần thành trì
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        [Header("Owner")]
        [Tooltip("Spawn point này thuộc về người chơi nào")]
        [SerializeField] private PlayerID owner;

        [Header("Grid Position")]
        [Tooltip("Vị trí trên grid (sẽ được tự động tính khi setup)")]
        [SerializeField] private Vector3Int gridPosition;

        [Header("Visual")]
        [SerializeField] private GameObject visualIndicator;
        [SerializeField] private Color availableColor = new Color(0, 1, 0, 0.3f);
        [SerializeField] private Color unavailableColor = new Color(1, 0, 0, 0.3f);

        // State
        private bool isOccupied = false;
        private Renderer indicatorRenderer;

        public PlayerID Owner => owner;
        public Vector3Int GridPosition => gridPosition;
        public bool IsOccupied => isOccupied;
        public bool IsAvailable => !isOccupied;

        private void Awake()
        {
            SetupVisualIndicator();
        }

        /// <summary>
        /// Setup spawn point với owner và grid position
        /// </summary>
        public void Initialize(PlayerID playerOwner, Vector3Int gridPos)
        {
            owner = playerOwner;
            gridPosition = gridPos;
            
            UpdateVisual();
        }

        /// <summary>
        /// Đặt vị trí grid cho spawn point
        /// </summary>
        public void SetGridPosition(Vector3Int gridPos)
        {
            gridPosition = gridPos;
        }

        /// <summary>
        /// Đánh dấu spawn point đã được sử dụng
        /// </summary>
        public void MarkAsOccupied()
        {
            isOccupied = true;
            UpdateVisual();
        }

        /// <summary>
        /// Đánh dấu spawn point trống
        /// </summary>
        public void MarkAsAvailable()
        {
            isOccupied = false;
            UpdateVisual();
        }

        /// <summary>
        /// Kiểm tra spawn point có thuộc về người chơi cụ thể không
        /// </summary>
        public bool BelongsTo(PlayerID player)
        {
            return owner == player;
        }

        /// <summary>
        /// Hiển thị/ẩn spawn point indicator
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (visualIndicator != null)
            {
                visualIndicator.SetActive(visible);
            }
        }

        private void SetupVisualIndicator()
        {
            if (visualIndicator != null)
            {
                indicatorRenderer = visualIndicator.GetComponent<Renderer>();
            }
            else
            {
                // Tạo indicator đơn giản nếu chưa có
                visualIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                visualIndicator.transform.SetParent(transform);
                visualIndicator.transform.localPosition = Vector3.zero;
                visualIndicator.transform.localScale = new Vector3(0.8f, 0.05f, 0.8f);
                
                indicatorRenderer = visualIndicator.GetComponent<Renderer>();
                
                // Xóa collider để không can thiệp input
                Destroy(visualIndicator.GetComponent<Collider>());
            }

            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (indicatorRenderer == null) return;

            // Đổi màu theo trạng thái
            Color color = isOccupied ? unavailableColor : availableColor;
            indicatorRenderer.material.color = color;
        }

        private void OnDrawGizmos()
        {
            // Hiển thị spawn point trong Scene view
            Gizmos.color = owner == PlayerID.Player1 ? Color.blue : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
