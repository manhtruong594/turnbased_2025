using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace TurnBasedGame.UI
{
    /// <summary>
    /// Singleton Screen Flow Controller.
    /// Quản lý stack-based navigation giữa các screen (prefab-based).
    /// Pattern: Singleton + Mediator + Stack-based FSM.
    /// </summary>
    public class UIManager : BaseManager
    {
        public static UIManager Instance { get; private set; }

        [Header("Screen Prefabs")]
        [Tooltip("Kéo tất cả screen prefab vào đây. Mỗi prefab phải có component kế thừa BaseScreen.")]
        [SerializeField] private List<BaseScreen> _screenPrefabs = new();

        [Header("Container")]
        [Tooltip("Parent transform để instantiate screens. Nếu null, dùng chính transform này.")]
        [SerializeField] private Transform _screenContainer;

        // ─── State ───

        private readonly Dictionary<Type, BaseScreen> _prefabRegistry = new();
        private readonly Stack<BaseScreen> _screenStack = new();
        private readonly List<BaseScreen> _activePopups = new();

        private BaseScreen _currentScreen;
        private bool _isTransitioning;

        // ─── Events ───

        /// <summary>Fired khi screen thay đổi. (previousScreen có thể null lần đầu)</summary>
        public event Action<BaseScreen, BaseScreen> OnScreenChanged;

        // ─── Lifecycle ───

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_screenContainer == null)
                _screenContainer = transform;

            BuildPrefabRegistry();
        }

        private void Start()
        {
            ShowScreen<MainMenuScreen>(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                HandleBackInput();
            if (Input.GetKeyDown(KeyCode.I))
                ShowScreen<MainMenuScreen>();
        }

        // ─── Public API ───

        /// <summary>
        /// Hiển thị screen theo type. Push lên stack, ẩn screen hiện tại.
        /// </summary>
        /// <typeparam name="T">Type của BaseScreen subclass trên prefab.</typeparam>
        /// <param name="useTransition">Có chạy animation transition không.</param>
        public void ShowScreen<T>(bool useTransition = true) where T : BaseScreen
        {
            var type = typeof(T);

            if (!_prefabRegistry.TryGetValue(type, out var prefab))
            {
                Debug.LogError($"[UIManager] Screen prefab '{type.Name}' chưa được đăng ký. Kiểm tra _screenPrefabs.");
                return;
            }

            if (_isTransitioning) return;

            StartCoroutine(TransitionToScreen(prefab, useTransition));
        }

        /// <summary>
        /// Quay lại screen trước đó trong stack (pop current, show previous).
        /// </summary>
        public void GoBack(bool useTransition = true)
        {
            if (_isTransitioning || _screenStack.Count <= 1) return;
            StartCoroutine(GoBackRoutine(useTransition));
        }

        /// <summary>
        /// Hiển thị popup overlay (không thay thế screen hiện tại).
        /// </summary>
        public T ShowPopup<T>(bool useTransition = true) where T : BaseScreen
        {
            var type = typeof(T);

            if (!_prefabRegistry.TryGetValue(type, out var prefab))
            {
                Debug.LogError($"[UIManager] Popup prefab '{type.Name}' chưa được đăng ký.");
                return null;
            }

            var instance = InstantiateScreen(prefab);

            if (useTransition)
                StartCoroutine(ShowWithTransition(instance));
            else
                instance.Show();

            _activePopups.Add(instance);
            return instance as T;
        }

        /// <summary>
        /// Đóng popup cụ thể.
        /// </summary>
        public void HidePopup(BaseScreen popup, bool useTransition = true)
        {
            if (popup == null || !_activePopups.Contains(popup)) return;

            if (useTransition)
                StartCoroutine(HideAndDestroyPopup(popup));
            else
            {
                _activePopups.Remove(popup);
                popup.Hide();
                Destroy(popup.gameObject);
            }
        }

        /// <summary>
        /// Đóng tất cả popup đang mở.
        /// </summary>
        public void HideAllPopups()
        {
            for (int i = _activePopups.Count - 1; i >= 0; i--)
            {
                var popup = _activePopups[i];
                popup.Hide();
                Destroy(popup.gameObject);
            }
            _activePopups.Clear();
        }

        /// <summary>
        /// Xóa toàn bộ stack và hiển thị screen mới (dùng khi chuyển scene/reset flow).
        /// </summary>
        public void ClearAndShow<T>(bool useTransition = true) where T : BaseScreen
        {
            HideAllPopups();
            ClearStack();
            ShowScreen<T>(useTransition);
        }

        /// <summary>Screen đang hiển thị (không tính popup).</summary>
        public BaseScreen CurrentScreen => _currentScreen;

        /// <summary>Số screen trong stack.</summary>
        public int StackDepth => _screenStack.Count;

        /// <summary>Có đang chạy transition animation không.</summary>
        public bool IsTransitioning => _isTransitioning;

        // ─── Internals ───

        private void BuildPrefabRegistry()
        {
            _prefabRegistry.Clear();

            foreach (var prefab in _screenPrefabs)
            {
                if (prefab == null)
                {
                    Debug.LogWarning("[UIManager] Null prefab trong _screenPrefabs. Bỏ qua.");
                    continue;
                }

                var type = prefab.GetType();
                if (_prefabRegistry.ContainsKey(type))
                {
                    Debug.LogWarning($"[UIManager] Trùng screen type '{type.Name}'. Chỉ giữ entry đầu tiên.");
                    continue;
                }

                _prefabRegistry[type] = prefab;
            }
        }

        private BaseScreen InstantiateScreen(BaseScreen prefab)
        {
            var instance = Instantiate(prefab, _screenContainer);
            instance.name = prefab.GetType().Name;
            instance.InjectDependencies(this);
            instance.gameObject.SetActive(false);
            return instance;
        }

        private IEnumerator TransitionToScreen(BaseScreen prefab, bool useTransition)
        {
            _isTransitioning = true;

            var previousScreen = _currentScreen;

            // Hide current screen
            if (previousScreen != null)
            {
                if (useTransition)
                    yield return StartCoroutine(previousScreen.PlayHideAnimation());

                previousScreen.Hide();
            }

            // Instantiate & show new screen
            var newScreen = InstantiateScreen(prefab);
            newScreen.Show();

            if (useTransition)
                yield return StartCoroutine(newScreen.PlayShowAnimation());

            // Update stack
            _screenStack.Push(newScreen);
            _currentScreen = newScreen;

            OnScreenChanged?.Invoke(previousScreen, newScreen);
            _isTransitioning = false;
        }

        private IEnumerator GoBackRoutine(bool useTransition)
        {
            _isTransitioning = true;

            // Pop & destroy current
            var toRemove = _screenStack.Pop();

            if (useTransition)
                yield return StartCoroutine(toRemove.PlayHideAnimation());

            toRemove.Hide();
            Destroy(toRemove.gameObject);

            // Restore previous
            var previousScreen = _screenStack.Count > 0 ? _screenStack.Peek() : null;

            if (previousScreen != null)
            {
                previousScreen.Show();

                if (useTransition)
                    yield return StartCoroutine(previousScreen.PlayShowAnimation());
            }

            var oldScreen = _currentScreen;
            _currentScreen = previousScreen;

            OnScreenChanged?.Invoke(oldScreen, _currentScreen);
            _isTransitioning = false;
        }

        private IEnumerator ShowWithTransition(BaseScreen screen)
        {
            screen.Show();
            yield return StartCoroutine(screen.PlayShowAnimation());
        }

        private IEnumerator HideAndDestroyPopup(BaseScreen popup)
        {
            yield return StartCoroutine(popup.PlayHideAnimation());
            _activePopups.Remove(popup);
            popup.Hide();
            Destroy(popup.gameObject);
        }

        private void HandleBackInput()
        {
            // Đóng popup trước
            if (_activePopups.Count > 0)
            {
                var topPopup = _activePopups[^1];
                if (topPopup.OnBackPressed())
                    HidePopup(topPopup);
                return;
            }

            // Sau đó mới pop screen stack
            if (_currentScreen != null && _currentScreen.OnBackPressed())
                GoBack();
        }

        private void ClearStack()
        {
            while (_screenStack.Count > 0)
            {
                var screen = _screenStack.Pop();
                screen.Hide();
                Destroy(screen.gameObject);
            }
            _currentScreen = null;
        }

        private void OnDestroy()
        {
            ClearStack();
            HideAllPopups();
        }
    }
}
