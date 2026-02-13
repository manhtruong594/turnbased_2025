using UnityEngine.UIElements;
using TurnBasedGame.Unit;

namespace TurnBasedGame.UI
{
    /// <summary>
    /// Hiển thị chi tiết unit trong Inventory: icon lớn, stats, mô tả, trạng thái deck.
    /// Bind vào các element có sẵn trong Inventory.uxml (section unit-detail).
    /// </summary>
    public class UnitDetailPanel
    {
        private readonly VisualElement _root;
        private readonly VisualElement _icon;
        private readonly Label _name;
        private readonly Label _desc;
        private readonly Label _hp;
        private readonly Label _cost;
        private readonly Label _move;
        private readonly Label _speed;
        private readonly Label _deckStatus;

        public UnitDetailPanel(VisualElement root)
        {
            _root = root;
            _icon = root.Q("unit-icon-large");
            _name = root.Q<Label>("unit-detail-name");
            _desc = root.Q<Label>("unit-detail-desc");
            _hp = root.Q<Label>("unit-stat-hp");
            _cost = root.Q<Label>("unit-stat-cost");
            _move = root.Q<Label>("unit-stat-move");
            _speed = root.Q<Label>("unit-stat-speed");
            _deckStatus = root.Q<Label>("unit-in-deck-label");
        }

        /// <summary>Bind dữ liệu UnitData vào detail panel.</summary>
        public void Bind(UnitData unit, bool isInDeck)
        {
            if (unit == null) return;

            if (unit.icon != null)
                _icon.style.backgroundImage = new StyleBackground(unit.icon);
            else
                _icon.style.backgroundImage = StyleKeyword.None;

            SetText(_name, unit.unitName);
            SetText(_desc, string.IsNullOrEmpty(unit.description) ? "Không có mô tả." : unit.description);
            SetText(_hp, unit.Health.ToString());
            SetText(_cost, unit.spawnCost.ToString());
            SetText(_move, unit.moveRange.ToString());
            SetText(_speed, $"{unit.moveSpeed:F1}");
            SetText(_deckStatus, isInDeck ? "✔ Đang trong đội hình" : "");
        }

        public void Show() => _root.style.display = DisplayStyle.Flex;
        public void Hide() => _root.style.display = DisplayStyle.None;

        private static void SetText(Label label, string text)
        {
            if (label != null) label.text = text;
        }
    }
}
