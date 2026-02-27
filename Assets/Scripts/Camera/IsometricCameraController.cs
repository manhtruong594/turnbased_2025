using UnityEngine;

/// <summary>
/// Camera controller cho game isometric turn-based.
/// Hỗ trợ: follow unit, pan (kéo chuột), zoom (scroll), clamp bounds.
/// 
/// === THIẾT LẬP TRONG INSPECTOR ===
/// Camera Transform:
///   Rotation: X=30, Y=45, Z=0  (classic isometric)
///   Camera component: Perspective, FOV=40-50
/// 
/// Hoặc dùng Orthographic cho pixel-art style:
///   Camera component: Orthographic, Size=8
/// </summary>
public class IsometricCameraController : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private float followSmoothSpeed = 5f;
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 10f, -8f);

    [Header("Pan (Right-click drag)")]
    [SerializeField] private float panSpeedMouse = 0.3f;
    [SerializeField] private float panSpeedKeyboard = 10f;
    [SerializeField] private KeyCode panModifierKey = KeyCode.Mouse1; // Right-click

    [Header("Zoom (Scroll wheel)")]
    [SerializeField] private float zoomSpeed = 3f;
    [SerializeField] private float zoomMin = 4f;
    [SerializeField] private float zoomMax = 20f;
    [SerializeField] private float zoomSmoothSpeed = 8f;

    [Header("Bounds (leave at 0 to disable)")]
    [SerializeField] private Vector2 boundsCenter;
    [SerializeField] private Vector2 boundsSize = Vector2.zero;

    // Internal state
    private Vector3 _panVelocity;
    private Vector3 _dragOrigin;
    private Vector3 _posAtDragStart;
    private float _targetZoom;
    private Camera _cam;
    private bool _isPanning;

    private void Awake()
    {
        _cam = GetComponentInChildren<Camera>();
        _targetZoom = GetCurrentZoom();
    }

    private void LateUpdate()
    {
        HandleFollow();
        HandlePan();
        HandleZoom();
        ApplyBounds();
    }

    // ── Follow ────────────────────────────────────────────────────────────────

    public void SetFollowTarget(Transform target) => followTarget = target;

    private void HandleFollow()
    {
        if (followTarget == null || _isPanning) return;

        var desired = followTarget.position + GetWorldOffset();
        transform.position = Vector3.Lerp(transform.position, desired, followSmoothSpeed * Time.deltaTime);
    }

    /// <summary>Chuyển followOffset (local pivot) sang world space dựa trên rotation Y của camera.</summary>
    private Vector3 GetWorldOffset()
    {
        var rot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        return rot * followOffset;
    }

    // ── Pan ───────────────────────────────────────────────────────────────────

    private void HandlePan()
    {
        // Keyboard pan (WASD / Arrow keys)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
        {
            followTarget = null;
            var dir = GetFlatForward() * v + GetFlatRight() * h;
            transform.position += dir * (panSpeedKeyboard * Time.deltaTime);
        }

        // Mouse drag pan
        if (Input.GetKeyDown(panModifierKey))
        {
            _dragOrigin = Input.mousePosition;
            _posAtDragStart = transform.position;
            _isPanning = true;
        }
        if (Input.GetKey(panModifierKey) && _isPanning)
        {
            var delta = Input.mousePosition - _dragOrigin;
            var move = (GetFlatRight() * -delta.x + GetFlatForward() * -delta.y) * panSpeedMouse;
            transform.position = _posAtDragStart + move;
            if (delta.magnitude > 5f) followTarget = null;
        }
        if (Input.GetKeyUp(panModifierKey))
            _isPanning = false;
    }

    private Vector3 GetFlatForward()
    {
        var fwd = transform.forward;
        fwd.y = 0f;
        return fwd.normalized;
    }

    private Vector3 GetFlatRight()
    {
        var right = transform.right;
        right.y = 0f;
        return right.normalized;
    }

    // ── Zoom ──────────────────────────────────────────────────────────────────

    private void HandleZoom()
    {
        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
            _targetZoom = Mathf.Clamp(_targetZoom - scroll * zoomSpeed * 10f, zoomMin, zoomMax);

        if (_cam.orthographic)
        {
            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetZoom, zoomSmoothSpeed * Time.deltaTime);
        }
        else
        {
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, _targetZoom, zoomSmoothSpeed * Time.deltaTime);
        }
    }

    private float GetCurrentZoom() => _cam != null
        ? (_cam.orthographic ? _cam.orthographicSize : _cam.fieldOfView)
        : 10f;

    // ── Bounds ────────────────────────────────────────────────────────────────

    private void ApplyBounds()
    {
        if (boundsSize == Vector2.zero) return;

        var p = transform.position;
        p.x = Mathf.Clamp(p.x, boundsCenter.x - boundsSize.x * 0.5f, boundsCenter.x + boundsSize.x * 0.5f);
        p.z = Mathf.Clamp(p.z, boundsCenter.y - boundsSize.y * 0.5f, boundsCenter.y + boundsSize.y * 0.5f);
        transform.position = p;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (boundsSize == Vector2.zero) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(new Vector3(boundsCenter.x, 0f, boundsCenter.y),
                            new Vector3(boundsSize.x, 0.1f, boundsSize.y));
    }
#endif
}
