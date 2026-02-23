using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;

namespace TurnBasedGame.UI
{
    /// <summary>
    /// Quản lý popup overlays: confirm dialogs, notifications, modal panels.
    /// Tách riêng khỏi UIManager screen stack — popup luôn hiển thị trên top.
    /// Pattern: Singleton, dùng UI Toolkit UIDocument riêng cho popup layer.
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        public static PopupManager Instance { get; private set; }

        [Header("Popup Layer")]
        [Tooltip("UIDocument riêng cho popup overlay. Sort order cao hơn screen.")]
        [SerializeField] private UIDocument _popupDocument;

        [Header("Defaults")]
        [SerializeField, Range(1f, 10f)] private float _defaultNotificationDuration = 3f;
        [SerializeField, Range(0.05f, 1f)] private float _fadeDuration = 0.2f;

        private VisualElement _popupRoot;
        private readonly List<VisualElement> _activeNotifications = new();

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
        }

        private void Start()
        {
            InitializePopupRoot();
        }

        // ─── Public API ───

        /// <summary>
        /// Hiển thị confirm dialog với 2 nút Xác Nhận / Hủy.
        /// </summary>
        /// <param name="title">Tiêu đề dialog.</param>
        /// <param name="message">Nội dung mô tả.</param>
        /// <param name="onConfirm">Callback khi nhấn Xác Nhận.</param>
        /// <param name="onCancel">Callback khi nhấn Hủy (có thể null).</param>
        /// <param name="confirmText">Text nút xác nhận (default: "Xác Nhận").</param>
        /// <param name="cancelText">Text nút hủy (default: "Hủy").</param>
        public void ShowConfirmDialog(
            string title,
            string message,
            Action onConfirm,
            Action onCancel = null,
            string confirmText = "Xác Nhận",
            string cancelText = "Hủy")
        {
            if (_popupRoot == null) return;

            var overlay = CreateOverlay();
            var dialog = CreateDialogContainer();

            // Title
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("popup-title");
            titleLabel.style.fontSize = 24;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 12;
            titleLabel.style.color = new Color(0.95f, 0.9f, 0.8f);
            dialog.Add(titleLabel);

            // Message
            var messageLabel = new Label(message);
            messageLabel.AddToClassList("popup-message");
            messageLabel.style.fontSize = 16;
            messageLabel.style.marginBottom = 20;
            messageLabel.style.whiteSpace = WhiteSpace.Normal;
            messageLabel.style.color = new Color(0.85f, 0.82f, 0.75f);
            dialog.Add(messageLabel);

            // Buttons container
            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.justifyContent = Justify.Center;

            var confirmBtn = CreateButton(confirmText, "popup-btn-confirm");
            confirmBtn.style.backgroundColor = new Color(0.2f, 0.55f, 0.3f);
            confirmBtn.clicked += () =>
            {
                onConfirm?.Invoke();
                StartCoroutine(DismissOverlay(overlay));
            };

            var cancelBtn = CreateButton(cancelText, "popup-btn-cancel");
            cancelBtn.style.backgroundColor = new Color(0.55f, 0.2f, 0.2f);
            cancelBtn.clicked += () =>
            {
                onCancel?.Invoke();
                StartCoroutine(DismissOverlay(overlay));
            };

            buttonRow.Add(confirmBtn);
            buttonRow.Add(cancelBtn);
            dialog.Add(buttonRow);

            overlay.Add(dialog);
            _popupRoot.Add(overlay);

            StartCoroutine(FadeIn(overlay));
        }

        /// <summary>
        /// Hiển thị info dialog với 1 nút OK.
        /// </summary>
        public void ShowInfoDialog(string title, string message, Action onClose = null, string closeText = "OK")
        {
            if (_popupRoot == null) return;

            var overlay = CreateOverlay();
            var dialog = CreateDialogContainer();

            var titleLabel = new Label(title);
            titleLabel.AddToClassList("popup-title");
            titleLabel.style.fontSize = 24;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 12;
            titleLabel.style.color = new Color(0.95f, 0.9f, 0.8f);
            dialog.Add(titleLabel);

            var messageLabel = new Label(message);
            messageLabel.AddToClassList("popup-message");
            messageLabel.style.fontSize = 16;
            messageLabel.style.marginBottom = 20;
            messageLabel.style.whiteSpace = WhiteSpace.Normal;
            messageLabel.style.color = new Color(0.85f, 0.82f, 0.75f);
            dialog.Add(messageLabel);

            var okBtn = CreateButton(closeText, "popup-btn-ok");
            okBtn.style.backgroundColor = new Color(0.3f, 0.45f, 0.6f);
            okBtn.style.alignSelf = Align.Center;
            okBtn.clicked += () =>
            {
                onClose?.Invoke();
                StartCoroutine(DismissOverlay(overlay));
            };

            dialog.Add(okBtn);
            overlay.Add(dialog);
            _popupRoot.Add(overlay);

            StartCoroutine(FadeIn(overlay));
        }

        /// <summary>
        /// Hiển thị notification tạm thời (toast) ở góc trên.
        /// Tự biến mất sau duration giây.
        /// </summary>
        public void ShowNotification(string message, float duration = 0f)
        {
            if (_popupRoot == null) return;
            if (duration <= 0f) duration = _defaultNotificationDuration;

            var toast = new VisualElement();
            toast.AddToClassList("popup-notification");
            toast.style.position = Position.Absolute;
            toast.style.top = 20 + _activeNotifications.Count * 60;
            toast.style.left = Length.Percent(50f);
            toast.style.translate = new Translate(Length.Percent(-50f), 0f);
            toast.style.backgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.9f);
            toast.style.borderTopLeftRadius = 8;
            toast.style.borderTopRightRadius = 8;
            toast.style.borderBottomLeftRadius = 8;
            toast.style.borderBottomRightRadius = 8;
            toast.style.paddingTop = 10;
            toast.style.paddingBottom = 10;
            toast.style.paddingLeft = 24;
            toast.style.paddingRight = 24;

            var label = new Label(message);
            label.style.fontSize = 14;
            label.style.color = new Color(0.95f, 0.93f, 0.88f);
            toast.Add(label);

            _popupRoot.Add(toast);
            _activeNotifications.Add(toast);

            StartCoroutine(NotificationLifecycle(toast, duration));
        }

        /// <summary>Đóng tất cả popup đang hiển thị.</summary>
        public void DismissAll()
        {
            if (_popupRoot == null) return;
            _popupRoot.Clear();
            _activeNotifications.Clear();
        }

        /// <summary>Có popup nào đang hiển thị không.</summary>
        public bool HasActivePopup => _popupRoot != null && _popupRoot.childCount > 0;

        // ─── Internals ───

        private void InitializePopupRoot()
        {
            if (_popupDocument != null)
            {
                // Đảm bảo popup luôn trên top
                _popupDocument.sortingOrder = 1000;
                _popupRoot = _popupDocument.rootVisualElement;
            }
            else
            {
                // Fallback: tạo runtime UIDocument
                var doc = gameObject.AddComponent<UIDocument>();
                doc.sortingOrder = 1000; // luôn trên top
                _popupRoot = doc.rootVisualElement;
                _popupDocument = doc;
            }

            _popupRoot.style.position = Position.Absolute;
            _popupRoot.style.top = 0;
            _popupRoot.style.bottom = 0;
            _popupRoot.style.left = 0;
            _popupRoot.style.right = 0;
            _popupRoot.pickingMode = PickingMode.Ignore;
        }

        private VisualElement CreateOverlay()
        {
            var overlay = new VisualElement();
            overlay.AddToClassList("popup-overlay");
            overlay.style.position = Position.Absolute;
            overlay.style.top = 0;
            overlay.style.bottom = 0;
            overlay.style.left = 0;
            overlay.style.right = 0;
            overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;
            overlay.style.opacity = 0f;
            overlay.pickingMode = PickingMode.Position; // block input bên dưới
            return overlay;
        }

        private VisualElement CreateDialogContainer()
        {
            var dialog = new VisualElement();
            dialog.AddToClassList("popup-dialog");
            dialog.style.backgroundColor = new Color(0.12f, 0.1f, 0.15f, 0.95f);
            dialog.style.borderTopLeftRadius = 12;
            dialog.style.borderTopRightRadius = 12;
            dialog.style.borderBottomLeftRadius = 12;
            dialog.style.borderBottomRightRadius = 12;
            dialog.style.paddingTop = 24;
            dialog.style.paddingBottom = 24;
            dialog.style.paddingLeft = 32;
            dialog.style.paddingRight = 32;
            dialog.style.minWidth = 300;
            dialog.style.maxWidth = 500;
            dialog.style.alignItems = Align.Stretch;

            // Border
            dialog.style.borderTopWidth = 2;
            dialog.style.borderBottomWidth = 2;
            dialog.style.borderLeftWidth = 2;
            dialog.style.borderRightWidth = 2;
            dialog.style.borderTopColor = new Color(0.6f, 0.5f, 0.3f, 0.8f);
            dialog.style.borderBottomColor = new Color(0.6f, 0.5f, 0.3f, 0.8f);
            dialog.style.borderLeftColor = new Color(0.6f, 0.5f, 0.3f, 0.8f);
            dialog.style.borderRightColor = new Color(0.6f, 0.5f, 0.3f, 0.8f);

            return dialog;
        }

        private Button CreateButton(string text, string className)
        {
            var btn = new Button { text = text };
            btn.AddToClassList(className);
            btn.style.fontSize = 16;
            btn.style.paddingTop = 8;
            btn.style.paddingBottom = 8;
            btn.style.paddingLeft = 24;
            btn.style.paddingRight = 24;
            btn.style.marginLeft = 8;
            btn.style.marginRight = 8;
            btn.style.borderTopLeftRadius = 6;
            btn.style.borderTopRightRadius = 6;
            btn.style.borderBottomLeftRadius = 6;
            btn.style.borderBottomRightRadius = 6;
            btn.style.color = new Color(0.95f, 0.93f, 0.88f);
            return btn;
        }

        private IEnumerator FadeIn(VisualElement element)
        {
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                element.style.opacity = Mathf.Clamp01(elapsed / _fadeDuration);
                yield return null;
            }
            element.style.opacity = 1f;
        }

        private IEnumerator DismissOverlay(VisualElement overlay)
        {
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                overlay.style.opacity = 1f - Mathf.Clamp01(elapsed / _fadeDuration);
                yield return null;
            }
            overlay.RemoveFromHierarchy();
        }

        private IEnumerator NotificationLifecycle(VisualElement toast, float duration)
        {
            // Fade in
            yield return FadeIn(toast);

            // Wait
            yield return new WaitForSecondsRealtime(duration);

            // Fade out
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                toast.style.opacity = 1f - Mathf.Clamp01(elapsed / _fadeDuration);
                yield return null;
            }

            _activeNotifications.Remove(toast);
            toast.RemoveFromHierarchy();
        }
    }
}
