using UnityEngine;
using TurnBasedGame.Core;

namespace TurnBasedGame.Capture
{
    /// <summary>
    /// Component đặt lên mỗi cứ điểm trên bản đồ.
    /// Quản lý trạng thái sở hữu và visual feedback (đổi màu cờ/ánh sáng) khi bị chiếm.
    /// </summary>
    public class CapturePoint : MonoBehaviour
    {
        [Header("Visual")]
        [Tooltip("Renderer của cờ/banner trên cứ điểm")]
        [SerializeField] private Renderer _flagRenderer;
        [Tooltip("Đèn chiếu sáng cứ điểm (tuỳ chọn)")]
        [SerializeField] private Light _pointLight;

        [Header("Colors")]
        [SerializeField] private Color _neutralColor = Color.white;
        [SerializeField] private Color _player1Color = new Color(0.2f, 0.5f, 1f);
        [SerializeField] private Color _player2Color = new Color(1f, 0.3f, 0.3f);

        private Vector3Int _gridPosition;
        private PlayerID? _owner;

        #region Properties
        public Vector3Int GridPosition => _gridPosition;
        public PlayerID? Owner => _owner;
        public bool IsNeutral => !_owner.HasValue;
        public bool IsCapturedBy(PlayerID player) => _owner == player;
        #endregion

        private void Awake()
        {
            if (_flagRenderer == null)
                CreateDefaultIndicator();
        }

        /// <summary>
        /// Khởi tạo cứ điểm với vị trí grid tương ứng
        /// </summary>
        public void Initialize(Vector3Int gridPos)
        {
            _gridPosition = gridPos;
            _owner = null;
            UpdateVisual();
        }

        /// <summary>
        /// Chiếm cứ điểm cho phe. Trả về true nếu đổi chủ thành công.
        /// </summary>
        public bool TryCapture(PlayerID player)
        {
            if (_owner == player) return false;

            var previous = _owner;
            _owner = player;
            UpdateVisual();

            Debug.Log($"[CapturePoint] {_gridPosition}: " +
                      $"{(previous.HasValue ? previous.ToString() : "Neutral")} → {player}");
            return true;
        }

        private Color GetOwnerColor()
        {
            return _owner switch
            {
                PlayerID.Player1 => _player1Color,
                PlayerID.Player2 => _player2Color,
                _ => _neutralColor
            };
        }

        private void UpdateVisual()
        {
            var color = GetOwnerColor();

            if (_flagRenderer != null)
                _flagRenderer.material.color = color;

            if (_pointLight != null)
                _pointLight.color = color;
        }

        /// <summary>
        /// Tạo indicator mặc định nếu chưa gán Renderer
        /// </summary>
        private void CreateDefaultIndicator()
        {
            var indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicator.transform.SetParent(transform);
            indicator.transform.localPosition = Vector3.zero;
            indicator.transform.localScale = new Vector3(1f, 0.05f, 1f);

            _flagRenderer = indicator.GetComponent<Renderer>();
            Destroy(indicator.GetComponent<Collider>());
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = _owner.HasValue ? GetOwnerColor() : _neutralColor;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, new Vector3(0.8f, 1f, 0.8f));
        }
#endif
    }
}
