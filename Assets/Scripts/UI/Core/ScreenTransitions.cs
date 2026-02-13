using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace TurnBasedGame.UI
{
    /// <summary>
    /// Fade in/out transition. Default cho mọi screen.
    /// </summary>
    [CreateAssetMenu(menuName = "TurnBasedGame/UI/Transitions/Fade", fileName = "FadeTransition")]
    public class FadeTransition : ScriptableObject, IScreenTransition
    {
        [SerializeField, Range(0.05f, 2f)] private float _duration = 0.25f;

        public float Duration => _duration;

        public IEnumerator TransitionIn(VisualElement root)
        {
            if (root == null) yield break;

            root.style.opacity = 0f;
            float elapsed = 0f;

            while (elapsed < _duration)
            {
                elapsed += Time.unscaledDeltaTime;
                root.style.opacity = Mathf.Clamp01(elapsed / _duration);
                yield return null;
            }

            root.style.opacity = 1f;
        }

        public IEnumerator TransitionOut(VisualElement root)
        {
            if (root == null) yield break;

            root.style.opacity = 1f;
            float elapsed = 0f;

            while (elapsed < _duration)
            {
                elapsed += Time.unscaledDeltaTime;
                root.style.opacity = 1f - Mathf.Clamp01(elapsed / _duration);
                yield return null;
            }

            root.style.opacity = 0f;
        }
    }

    /// <summary>
    /// Slide transition — trượt vào/ra theo hướng chỉ định.
    /// </summary>
    [CreateAssetMenu(menuName = "TurnBasedGame/UI/Transitions/Slide", fileName = "SlideTransition")]
    public class SlideTransition : ScriptableObject, IScreenTransition
    {
        public enum SlideDirection { Left, Right, Up, Down }

        [SerializeField, Range(0.05f, 2f)] private float _duration = 0.3f;
        [SerializeField] private SlideDirection _direction = SlideDirection.Right;
        [SerializeField] private AnimationCurve _curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public float Duration => _duration;

        public IEnumerator TransitionIn(VisualElement root)
        {
            if (root == null) yield break;

            var offset = GetOffset();
            root.style.translate = new Translate(offset.x, offset.y);
            root.style.opacity = 1f;
            float elapsed = 0f;

            while (elapsed < _duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _curve.Evaluate(Mathf.Clamp01(elapsed / _duration));
                float x = Mathf.Lerp(offset.x, 0f, t);
                float y = Mathf.Lerp(offset.y, 0f, t);
                root.style.translate = new Translate(x, y);
                yield return null;
            }

            root.style.translate = new Translate(0f, 0f);
        }

        public IEnumerator TransitionOut(VisualElement root)
        {
            if (root == null) yield break;

            var offset = GetOffset();
            // Slide ra hướng ngược lại
            offset = new Vector2(-offset.x, -offset.y);
            float elapsed = 0f;

            while (elapsed < _duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _curve.Evaluate(Mathf.Clamp01(elapsed / _duration));
                float x = Mathf.Lerp(0f, offset.x, t);
                float y = Mathf.Lerp(0f, offset.y, t);
                root.style.translate = new Translate(x, y);
                yield return null;
            }

            root.style.translate = new Translate(offset.x, offset.y);
        }

        private Vector2 GetOffset()
        {
            return _direction switch
            {
                SlideDirection.Left  => new Vector2(-1920f, 0f),
                SlideDirection.Right => new Vector2(1920f, 0f),
                SlideDirection.Up    => new Vector2(0f, -1080f),
                SlideDirection.Down  => new Vector2(0f, 1080f),
                _ => new Vector2(1920f, 0f)
            };
        }
    }

    /// <summary>
    /// Scroll Unfurl — cuộn giấy mở ra/đóng lại, phù hợp theme Đại Nam.
    /// Dùng scaleY + opacity để tạo hiệu ứng cuộn giấy.
    /// </summary>
    [CreateAssetMenu(menuName = "TurnBasedGame/UI/Transitions/Scroll Unfurl", fileName = "ScrollUnfurlTransition")]
    public class ScrollUnfurlTransition : ScriptableObject, IScreenTransition
    {
        [SerializeField, Range(0.1f, 2f)] private float _duration = 0.4f;
        [SerializeField] private AnimationCurve _curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public float Duration => _duration;

        public IEnumerator TransitionIn(VisualElement root)
        {
            if (root == null) yield break;

            root.style.scale = new Scale(new Vector3(1f, 0f, 1f));
            root.style.opacity = 0f;
            root.style.transformOrigin = new TransformOrigin(Length.Percent(50f), Length.Percent(0f));
            float elapsed = 0f;

            while (elapsed < _duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _curve.Evaluate(Mathf.Clamp01(elapsed / _duration));
                root.style.scale = new Scale(new Vector3(1f, t, 1f));
                root.style.opacity = t;
                yield return null;
            }

            root.style.scale = new Scale(Vector3.one);
            root.style.opacity = 1f;
        }

        public IEnumerator TransitionOut(VisualElement root)
        {
            if (root == null) yield break;

            root.style.transformOrigin = new TransformOrigin(Length.Percent(50f), Length.Percent(0f));
            float elapsed = 0f;

            while (elapsed < _duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = 1f - _curve.Evaluate(Mathf.Clamp01(elapsed / _duration));
                root.style.scale = new Scale(new Vector3(1f, t, 1f));
                root.style.opacity = t;
                yield return null;
            }

            root.style.scale = new Scale(new Vector3(1f, 0f, 1f));
            root.style.opacity = 0f;
        }
    }
}
