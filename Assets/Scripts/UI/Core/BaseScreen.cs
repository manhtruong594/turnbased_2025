using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace TurnBasedGame.UI
{
    /// <summary>
    /// Template Method Pattern: Base class cho mọi screen/popup trong game.
    /// Mỗi screen prefab gắn 1 subclass của BaseScreen trên root GameObject.
    /// </summary>
    public abstract class BaseScreen : MonoBehaviour
    {
        [Header("UI Toolkit")]
        [SerializeField] protected UIDocument _uiDocument;

        [Header("Transition")]
        [Tooltip("Transition SO cho screen này. Null = dùng default fade.")]
        [SerializeField] private ScriptableObject _transitionAsset;

        protected UIManager UIManager { get; private set; }
        protected VisualElement Root { get; private set; }

        /// <summary>Transition được gán (cast từ SO, phải implement IScreenTransition).</summary>
        public IScreenTransition Transition => _transitionAsset as IScreenTransition;

        /// <summary>True nếu screen đang hiển thị.</summary>
        public bool IsVisible { get; private set; }

        /// <summary>True nếu screen là popup (overlay, không thay thế screen bên dưới).</summary>
        public virtual bool IsPopup => false;

        // ─── Lifecycle (gọi bởi UIManager) ───

        internal void InjectDependencies(UIManager uiManager)
        {
            UIManager = uiManager;
        }

        internal void Show()
        {
            gameObject.SetActive(true);
            CacheRoot();
            IsVisible = true;
            OnScreenShow();
        }

        internal void Hide()
        {
            IsVisible = false;
            OnScreenHide();
            gameObject.SetActive(false);
        }

        // ─── Template Methods (override trong subclass) ───

        /// <summary>Được gọi khi screen xuất hiện. Dùng để bind UI elements, refresh data.</summary>
        protected virtual void OnScreenShow() { }

        /// <summary>Được gọi khi screen bị ẩn.</summary>
        protected virtual void OnScreenHide() { }

        /// <summary>Được gọi trước khi screen bị destroy.</summary>
        protected virtual void OnScreenDestroy() { }

        /// <summary>
        /// Xử lý nút Back / Android back.
        /// Return true = cho phép UIManager pop screen. False = tự xử lý.
        /// </summary>
        public virtual bool OnBackPressed() => true;

        // ─── Transition Hooks ───

        /// <summary>
        /// Animation khi screen xuất hiện.
        /// Ưu tiên: IScreenTransition SO → override subclass → default fade.
        /// </summary>
        public virtual IEnumerator PlayShowAnimation()
        {
            if (Root == null) yield break;

            if (Transition != null)
            {
                yield return Transition.TransitionIn(Root);
                yield break;
            }

            // Default fade in
            Root.style.opacity = 0f;
            float elapsed = 0f;
            const float duration = 0.25f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                Root.style.opacity = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            Root.style.opacity = 1f;
        }

        /// <summary>
        /// Animation khi screen biến mất.
        /// Ưu tiên: IScreenTransition SO → override subclass → default fade.
        /// </summary>
        public virtual IEnumerator PlayHideAnimation()
        {
            if (Root == null) yield break;

            if (Transition != null)
            {
                yield return Transition.TransitionOut(Root);
                yield break;
            }

            // Default fade out
            float elapsed = 0f;
            const float duration = 0.2f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                Root.style.opacity = 1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            Root.style.opacity = 0f;
        }

        // ─── Helpers ───

        private void CacheRoot()
        {
            if (_uiDocument != null)
                Root = _uiDocument.rootVisualElement;
        }

        /// <summary>Shortcut query UI Toolkit element by name.</summary>
        protected T Q<T>(string name = null) where T : VisualElement
        {
            return Root?.Q<T>(name);
        }

        /// <summary>Shortcut query UI Toolkit element by name (VisualElement).</summary>
        protected VisualElement Q(string name)
        {
            return Root?.Q(name);
        }

        private void OnDestroy()
        {
            OnScreenDestroy();
        }
    }
}
