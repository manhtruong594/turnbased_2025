using UnityEngine;
using TMPro;
using TurnBasedGame.ObjectPool;

namespace TurnBasedGame.UI
{
    /// <summary>
    /// Hiển thị số HP mất/thêm bay trên đầu unit trong world space.
    /// Damage: bay theo hình parabol. Heal: bay thẳng lên trên.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI tmpText;

        [Header("General Settings")]
        [SerializeField] private float duration = 1.2f;

        [Header("Damage Settings (Parabolic)")]
        [SerializeField] private Color damageColor = new Color(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private float parabolaHeight = 1.5f;
        [SerializeField] private float parabolaWidth = 0.6f;

        [Header("Heal Settings (Straight Up)")]
        [SerializeField] private Color healColor = new Color(0.2f, 1f, 0.3f, 1f);
        [SerializeField] private float healRiseHeight = 2f;

        [Header("Scale Punch")]
        [SerializeField] private float punchScale = 1.5f;
        [SerializeField] private float punchDuration = 0.15f;

        [Header("Font Size")]
        [SerializeField] private float baseFontSize = 3f;

        private Vector3 startWorldPos;
        private float elapsed;
        private bool isDamage;
        private bool isPlaying;
        private float horizontalDir;
        private Camera mainCamera;

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        /// <summary>
        /// Khởi tạo floating text với giá trị và loại (damage/heal).
        /// </summary>
        public void Setup(int amount, bool damage)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            isDamage = damage;
            elapsed = 0f;
            isPlaying = true;

            string prefix = damage ? "-" : "+";
            tmpText.text = string.Concat(prefix, amount.ToString());
            SetAlpha(1f);
            tmpText.color = damage ? damageColor : healColor;
            tmpText.fontSize = baseFontSize;
            transform.localScale = Vector3.one;
            startWorldPos = transform.position;
            horizontalDir = Random.value > 0.5f ? 1f : -1f;
        }

        private void Update()
        {
            if (!isPlaying) return;

            // Billboard: luôn quay mặt về phía camera
            if (mainCamera != null)
                transform.rotation = mainCamera.transform.rotation;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (isDamage)
            {
                float x = horizontalDir * parabolaWidth * t;
                float y = (-4f * parabolaHeight * t * t) + (4f * parabolaHeight * t);
                transform.position = startWorldPos + new Vector3(x, y, 0f);
            }
            else
            {
                float easedT = 1f - (1f - t) * (1f - t);
                transform.position = startWorldPos + new Vector3(0f, healRiseHeight * easedT, 0f);
            }

            if (t > 0.5f)
                SetAlpha(1f - (t - 0.5f) / 0.5f);

            if (elapsed < punchDuration)
            {
                float scale = Mathf.Lerp(punchScale, 1f, elapsed / punchDuration);
                transform.localScale = Vector3.one * scale;
            }

            if (t >= 1f)
            {
                isPlaying = false;
                var pooled = GetComponent<PooledObject>();
                if (pooled != null)
                    pooled.Despawn();
                else
                    Destroy(gameObject);
            }
        }

        private void SetAlpha(float alpha)
        {
            Color c = tmpText.color;
            c.a = alpha;
            tmpText.color = c;
        }

        private void OnEnable()
        {
            elapsed = 0f;
            isPlaying = false;
            if (tmpText != null) SetAlpha(1f);
            transform.localScale = Vector3.one;
        }
    }
}
