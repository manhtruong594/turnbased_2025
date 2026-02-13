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

        private VisualElement _root;
        private VisualElement _overlay;
        private ProgressBar _progressBar;
        private Label _loadingLabel;
        private bool _isLoading;

        /// <summary>True nếu đang trong quá trình load scene.</summary>
        public bool IsLoading => _isLoading;

        /// <summary>Fired khi scene load hoàn tất.</summary>
        public event Action<string> OnSceneLoaded;

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
            BuildLoadingUI();
        }

        // ─── Public API ───

        /// <summary>
        /// Load scene async với loading overlay + progress bar.
        /// </summary>
        /// <param name="sceneName">Tên scene cần load.</param>
        /// <param name="onProgress">Callback progress (0→1). Có thể null.</param>
        /// <param name="onComplete">Callback khi hoàn tất. Có thể null.</param>
        public void LoadSceneAsync(string sceneName, Action<float> onProgress = null, Action onComplete = null)
        {
            if (_isLoading)
            {
                Debug.LogWarning("[SceneLoader] Đang load scene, bỏ qua request mới.");
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName, LoadSceneMode.Single, onProgress, onComplete));
        }

        /// <summary>
        /// Load scene additive async (giữ scene hiện tại).
        /// </summary>
        public void LoadSceneAdditiveAsync(string sceneName, Action<float> onProgress = null, Action onComplete = null)
        {
            if (_isLoading) return;
            StartCoroutine(LoadSceneRoutine(sceneName, LoadSceneMode.Additive, onProgress, onComplete));
        }

        /// <summary>
        /// Unload scene đã load additive.
        /// </summary>
        public void UnloadSceneAsync(string sceneName, Action onComplete = null)
        {
            StartCoroutine(UnloadRoutine(sceneName, onComplete));
        }

        // ─── Core Routine ───

        private IEnumerator LoadSceneRoutine(string sceneName, LoadSceneMode mode, Action<float> onProgress, Action onComplete)
        {
            _isLoading = true;
            float startTime = Time.unscaledTime;

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
                onProgress?.Invoke(progress);
                yield return null;
            }

            UpdateProgress(1f, "Hoàn tất!");
            onProgress?.Invoke(1f);

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

        private void BuildLoadingUI()
        {
            if (_loadingDocument == null) return;

            _root = _loadingDocument.rootVisualElement;
            _root.style.position = Position.Absolute;
            _root.style.left = _root.style.top = _root.style.right = _root.style.bottom = 0;

            // Overlay background
            _overlay = new VisualElement { name = "loading-overlay" };
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = _overlay.style.top = _overlay.style.right = _overlay.style.bottom = 0;
            _overlay.style.backgroundColor = new Color(0.05f, 0.03f, 0.02f, 1f);
            _overlay.style.alignItems = Align.Center;
            _overlay.style.justifyContent = Justify.Center;
            _overlay.style.display = DisplayStyle.None;

            // Container
            var container = new VisualElement { name = "loading-container" };
            container.style.alignItems = Align.Center;
            container.style.width = Length.Percent(60);

            // Loading text
            _loadingLabel = new Label("Đang tải...") { name = "loading-label" };
            _loadingLabel.style.fontSize = 22;
            _loadingLabel.style.color = new Color(0.95f, 0.9f, 0.8f);
            _loadingLabel.style.marginBottom = 16;
            _loadingLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

            // Progress bar
            _progressBar = new ProgressBar { name = "loading-progress" };
            _progressBar.style.width = Length.Percent(100);
            _progressBar.style.height = 20;
            _progressBar.lowValue = 0;
            _progressBar.highValue = 100;
            _progressBar.value = 0;

            container.Add(_loadingLabel);
            container.Add(_progressBar);
            _overlay.Add(container);
            _root.Add(_overlay);

            // Start hidden
            _overlay.style.opacity = 0f;
        }

        private void UpdateProgress(float normalized, string text = null)
        {
            if (_progressBar != null)
                _progressBar.value = normalized * 100f;

            if (text != null && _loadingLabel != null)
                _loadingLabel.text = text;
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
