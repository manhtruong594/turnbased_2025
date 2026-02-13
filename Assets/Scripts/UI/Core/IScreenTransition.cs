using UnityEngine.UIElements;
using System.Collections;

namespace TurnBasedGame.UI
{
    /// <summary>
    /// Strategy Pattern: Interface cho screen transition animations.
    /// UIManager / BaseScreen dùng để chạy hiệu ứng chuyển cảnh.
    /// </summary>
    public interface IScreenTransition
    {
        /// <summary>Thời gian transition (seconds).</summary>
        float Duration { get; }

        /// <summary>Chạy animation xuất hiện trên root VisualElement.</summary>
        IEnumerator TransitionIn(VisualElement root);

        /// <summary>Chạy animation biến mất trên root VisualElement.</summary>
        IEnumerator TransitionOut(VisualElement root);
    }
}
