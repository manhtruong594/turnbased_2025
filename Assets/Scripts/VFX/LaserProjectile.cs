using UnityEngine;
using TurnBasedGame.ObjectPool;

namespace TurnBasedGame.VFX
{
    /// <summary>
    /// Laser projectile - tia laser bắn thẳng từ start đến target
    /// Dùng LineRenderer để vẽ beam, hỗ trợ fade-out sau khi đến target
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LaserProjectile : Projectile
    {
        [Header("Laser Settings")]
        [SerializeField] private float beamDuration = 0.3f;
        [SerializeField] private float startWidth = 0.2f;
        [SerializeField] private float endWidth = 0.05f;

        private LineRenderer _lineRenderer;
        private float _fadeTimer;
        private bool _isFading;
        private float _initialAlpha;

        protected override void OnEnable()
        {
            base.OnEnable();
            _isFading = false;
            _fadeTimer = 0f;
            InitLineRenderer();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _isFading = false;
            if (_lineRenderer != null)
                _lineRenderer.enabled = false;
        }

        public override void Launch(Vector3 start, Vector3 target, TrajectoryType type, float customSpeed = -1f, float customArcHeight = -1f)
        {
            _startPosition = start;
            _targetPosition = target;

            if (customSpeed > 0) speed = customSpeed;

            transform.position = start;
            _progress = 0f;
            _duration = Vector3.Distance(start, target) / speed;
            _isFlying = true;
            _isFading = false;

            InitLineRenderer();
            _lineRenderer.enabled = true;
            _lineRenderer.SetPosition(0, start);
            _lineRenderer.SetPosition(1, start);
        }

        protected override void Update()
        {
            if (_isFading)
            {
                UpdateFade();
                return;
            }

            if (!_isFlying) return;

            _progress += Time.deltaTime / _duration;

            // Beam kéo dài dần từ start đến target
            Vector3 currentEnd = Vector3.Lerp(_startPosition, _targetPosition, Mathf.Min(_progress, 1f));
            _lineRenderer.SetPosition(1, currentEnd);

            if (_progress >= 1f)
            {
                ReachTarget();
            }
        }

        protected override void ReachTarget()
        {
            _isFlying = false;
            _lineRenderer.SetPosition(1, _targetPosition);

            OnReachTarget?.Invoke();
            SpawnHitEffect();

            StartFade();
        }

        private void InitLineRenderer()
        {
            if (_lineRenderer != null) return;

            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.positionCount = 2;
            _lineRenderer.startWidth = startWidth;
            _lineRenderer.endWidth = endWidth;
            _lineRenderer.useWorldSpace = true;
        }

        private void StartFade()
        {
            _isFading = true;
            _fadeTimer = 0f;
            _initialAlpha = _lineRenderer.material.color.a;
        }

        private void UpdateFade()
        {
            _fadeTimer += Time.deltaTime;
            float t = _fadeTimer / beamDuration;

            if (t >= 1f)
            {
                _isFading = false;
                _lineRenderer.enabled = false;
                ObjectPoolManager.Instance.Despawn(pooledObject);
                return;
            }

            // Fade alpha
            Color color = _lineRenderer.material.color;
            color.a = Mathf.Lerp(_initialAlpha, 0f, t);
            _lineRenderer.material.color = color;

            // Thu nhỏ width
            float widthScale = 1f - t;
            _lineRenderer.startWidth = startWidth * widthScale;
            _lineRenderer.endWidth = endWidth * widthScale;
        }
    }
}
