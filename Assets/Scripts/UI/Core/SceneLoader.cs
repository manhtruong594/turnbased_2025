using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System;
using System.Collections;

namespace TurnBasedGame.UI
{
    /// <summary>
    /// Async scene loading với loading overlay (UI Toolkit).
    /// Singleton, DontDestroyOnLoad — persist qua scene transitions.
    /// Sử dụng UXML/USS template theo theme "Đại Nam".
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        [Header("Loading UI")]
        [Tooltip("UIDocument dành riêng cho loading overlay. Sort order cao nhất.")]
        [SerializeField] private UIDocument _loadingDocument;

        [Header("Settings")]
        [SerializeField, Range(0.3f, 3f)] private float _minDisplayTime = 0.5f;
        [SerializeField, Range(0.1f, 1f)] private float _fadeDuration = 0.3f;

        private VisualElement _overlay;
        private ProgressBar _progressBar;
        private Label _statusLabel;
        private Label _hintLabel;
        private bool _isLoading;

        /// <summary>True nếu đang trong quá trình load scene.</summary>
        public bool IsLoading => _isLoading;

        /// <summary>Fired khi scene load hoàn tất.</summary>
        public event Action<string> OnSceneLoaded;

        // ─── Loading Tips ───
        private static readonly string[] LoadingHints =
        {
            "Hãy bày binh bố trận cẩn thận trước mỗi trận chiến...",
            "Kết hợp kỹ năng đồng đội để tạo combo mạnh mẽ!",
            "Địa hình ảnh hưởng lớn đến chiến thuật — hãy tận dụng!",
            "Mỗi tướng có điểm mạnh riêng, hãy xây dựng đội hình hợp lý.",
            "Đừng quên nâng cấp trang bị trước khi ra trận!",
            "Quan sát lượt đi của đối thủ để dự đoán chiến thuật.",
        };

        // ─── Lifecycle ───

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            CacheUIReferences();
        }

        // ─── Public API ───

        /// <summary>
        /// Load scene async với loading overlay + progress bar.
        /// </summary>
        /// <param name="sceneName">Tên scene cần load.</param>
        /// <param name="onProgress">Callback progress (0→1). Có thể null.</param>
        /// <param name="onComplete">Callback khi hoàn tất. Có thể null.</param>
        public void LoadSceneAsync(string sceneName, Action onComplete = null)
        {
            if (_isLoading)
            {
                Debug.LogWarning("[SceneLoader] Đang load scene, bỏ qua request mới.");
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName, LoadSceneMode.Single, onComplete));
        }

        /// <summary>
        /// Load scene additive async (giữ scene hiện tại).
        /// </summary>
        public void LoadSceneAdditiveAsync(string sceneName, Action onComplete = null)
        {
            if (_isLoading) return;
            StartCoroutine(LoadSceneRoutine(sceneName, LoadSceneMode.Additive, onComplete));
        }

        /// <summary>
        /// Unload scene đã load additive.
        /// </summary>
        public void UnloadSceneAsync(string sceneName, Action onComplete = null)
        {
            StartCoroutine(UnloadRoutine(sceneName, onComplete));
        }

        // ─── Core Routine ───

        private IEnumerator LoadSceneRoutine(string sceneName, LoadSceneMode mode, Action onComplete)
        {
            _isLoading = true;
            float startTime = Time.unscaledTime;

            // Hiển thị hint ngẫu nhiên
            SetRandomHint();

            // Show overlay
            yield return FadeOverlay(true);
            UpdateProgress(0f, "Đang tải...");

            // Start async load
            var operation = SceneManager.LoadSceneAsync(sceneName, mode);
            operation.allowSceneActivation = false;

            // Progress loop (Unity caps progress at 0.9 until allowSceneActivation)
            while (operation.progress < 0.9f)
            {
                float progress = Mathf.Clamp01(operation.progress / 0.9f);
                UpdateProgress(progress);
                yield return null;
            }

            UpdateProgress(1f, "Hoàn tất!");

            // Đảm bảo overlay hiển thị tối thiểu _minDisplayTime
            float elapsed = Time.unscaledTime - startTime;
            if (elapsed < _minDisplayTime)
                yield return new WaitForSecondsRealtime(_minDisplayTime - elapsed);

            // Activate scene
            operation.allowSceneActivation = true;
            yield return operation;

            // Hide overlay
            yield return FadeOverlay(false);

            _isLoading = false;
            OnSceneLoaded?.Invoke(sceneName);
            onComplete?.Invoke();
        }

        private IEnumerator UnloadRoutine(string sceneName, Action onComplete)
        {
            var operation = SceneManager.UnloadSceneAsync(sceneName);
            if (operation != null)
                yield return operation;

            onComplete?.Invoke();
        }

        // ─── Loading UI ───

        private void CacheUIReferences()
        {
            if (_loadingDocument == null) return;

            var root = _loadingDocument.rootVisualElement;
            _overlay = root.Q<VisualElement>("loading-overlay");
            _progressBar = root.Q<ProgressBar>("loading-progress");
            _statusLabel = root.Q<Label>("loading-status");
            _hintLabel = root.Q<Label>("loading-hint");

            // Bắt đầu ẩn
            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.None;
                _overlay.style.opacity = 0f;
            }
        }

        private void SetRandomHint()
        {
            if (_hintLabel == null || LoadingHints.Length == 0) return;
            _hintLabel.text = LoadingHints[UnityEngine.Random.Range(0, LoadingHints.Length)];
        }

        private void UpdateProgress(float normalized, string text = null)
        {
            if (_progressBar != null)
                _progressBar.value = normalized * 100f;

            if (text != null && _statusLabel != null)
                _statusLabel.text = text;
        }

        private IEnumerator FadeOverlay(bool fadeIn)
        {
            if (_overlay == null) yield break;

            _overlay.style.display = DisplayStyle.Flex;
            float from = fadeIn ? 0f : 1f;
            float to = fadeIn ? 1f : 0f;
            float elapsed = 0f;

            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _fadeDuration);
                _overlay.style.opacity = Mathf.Lerp(from, to, t);
                yield return null;
            }

            _overlay.style.opacity = to;

            if (!fadeIn)
                _overlay.style.display = DisplayStyle.None;
        }
    }
}
