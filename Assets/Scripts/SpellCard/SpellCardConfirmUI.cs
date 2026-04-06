using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RedBjorn.ProtoTiles;

namespace TurnBasedGame.SpellCard
{
    /// <summary>
    /// UI xác nhận sử dụng Spell Card.
    /// Card bay lên giữa màn hình kèm nút Confirm (tick) / Cancel (cross).
    /// </summary>
    public class SpellCardConfirmUI : MonoBehaviour
    {
        [Header("Card Display")]
        [SerializeField] private RectTransform _cardRect;
        [SerializeField] private Image _cardIcon;
        [SerializeField] private TextMeshProUGUI _cardName;
        [SerializeField] private TextMeshProUGUI _cardDesc;
        [SerializeField] private TextMeshProUGUI _cardCost;

        [Header("Buttons")]
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private CanvasGroup _buttonsGroup;

        [Header("Overlay")]
        [SerializeField] private CanvasGroup _overlay;

        [Header("Animation")]
        [SerializeField] private float _flyDuration = 0.3f;
        [SerializeField] private AnimationCurve _flyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Canvas _rootCanvas;
        private Vector2 _designedPos;
        private Coroutine _animCoroutine;

        public event Action OnConfirmed;
        public event Action OnCanceled;

        private void Awake()
        {
            _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
            _designedPos = _cardRect.anchoredPosition;
            _confirmButton.onClick.AddListener(Confirm);
            _cancelButton.onClick.AddListener(Cancel);
        }

        public void Show(SpellCardData card, RectTransform sourceRect)
        {
            gameObject.SetActive(true);
            SetupCard(card);
            SetButtonsVisible(false);
            if (_overlay != null) _overlay.alpha = 0;

            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(AnimateFlyToCenter(sourceRect));
        }

        public void Hide()
        {
            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = null;
            gameObject.SetActive(false);
        }

        private void SetupCard(SpellCardData card)
        {
            if (_cardIcon != null) _cardIcon.sprite = card.icon;
            if (_cardName != null) _cardName.text = card.spellName;
            if (_cardDesc != null) _cardDesc.text = card.description;
            if (_cardCost != null) _cardCost.text = card.mpCost.ToString();
        }

        private void SetButtonsVisible(bool visible)
        {
            _buttonsGroup.alpha = visible ? 1f : 0f;
            _buttonsGroup.interactable = visible;
            _buttonsGroup.blocksRaycasts = visible;
        }

        private IEnumerator AnimateFlyToCenter(RectTransform sourceRect)
        {
            var parentRect = _cardRect.parent as RectTransform;
            Camera cam = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null : _rootCanvas.worldCamera;

            // Chuyển vị trí card gốc sang local space của card display parent
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, _cardRect.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, screenPos, cam, out var startPos);

            Vector3 startScale = Vector3.one * 0.6f;
            Vector3 endScale = Vector3.one;

            float elapsed = 0f;
            while (elapsed < _flyDuration)
            {
                elapsed += Time.deltaTime;
                float t = _flyCurve.Evaluate(Mathf.Clamp01(elapsed / _flyDuration));

                _cardRect.anchoredPosition = Vector2.Lerp(startPos, _designedPos, t);
                _cardRect.localScale = Vector3.Lerp(startScale, endScale, t);
                if (_overlay != null) _overlay.alpha = Mathf.Lerp(0f, 1, t);

                yield return null;
            }

            _cardRect.anchoredPosition = _designedPos;
            _cardRect.localScale = endScale;
            if (_overlay != null) _overlay.alpha = 1;
            SetButtonsVisible(true);
        }

        private void Confirm()
        {
            Hide();
            OnConfirmed?.Invoke();
        }

        private void Cancel()
        {
            Hide();
            OnCanceled?.Invoke();
        }
    }
}
