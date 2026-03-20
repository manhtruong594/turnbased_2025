using UnityEngine.UIElements;
using TurnBasedGame.Unit;
using TurnBasedGame.Skills;
using TurnBasedGame.SpellCard;

namespace TurnBasedGame.UI
{
    /// <summary>
    /// VisualElement cho mỗi slot trong deck/spell tray.
    /// Hiển thị icon + tên khi có data, placeholder khi trống.
    /// Click để remove item khỏi slot.
    /// </summary>
    [UxmlElement]
    public partial class DeckSlot : VisualElement
    {
        private const string UssClassName = "deck-slot";
        private const string UssEmpty = "deck-slot--empty";
        private const string UssFilled = "deck-slot--filled";

        private readonly VisualElement _icon;
        private readonly Label _label;
        private readonly Label _placeholder;

        public UnitController UnitData { get; private set; }
        public SpellCardData SpellData { get; private set; }
        public bool IsEmpty => UnitData == null && SpellData == null;

        public DeckSlot()
        {
            AddToClassList(UssClassName);
            AddToClassList(UssEmpty);

            _icon = new VisualElement();
            _icon.AddToClassList("deck-slot__icon");
            Add(_icon);

            _label = new Label();
            _label.AddToClassList("deck-slot__label");
            Add(_label);

            _placeholder = new Label { text = "+" };
            _placeholder.AddToClassList("deck-slot__placeholder");
            Add(_placeholder);

            UpdateVisual();
        }

        /// <summary>Gán unit vào slot.</summary>
        public void BindUnit(UnitController unit)
        {
            ClearSlot();
            UnitData = unit;
            if (unit == null) return;

            _label.text = unit.UnitData.unitName;
            if (unit.UnitData.icon != null)
                _icon.style.backgroundImage = new StyleBackground(unit.UnitData.icon);

            UpdateVisual();
        }

        /// <summary>Gán spell vào slot.</summary>
        public void BindSpell(SpellCardData spell)
        {
            ClearSlot();
            SpellData = spell;
            if (spell == null) return;

            _label.text = spell.spellName;
            if (spell.icon != null)
                _icon.style.backgroundImage = new StyleBackground(spell.icon);

            UpdateVisual();
        }

        /// <summary>Xóa data khỏi slot, về trạng thái trống.</summary>
        public void ClearSlot()
        {
            UnitData = null;
            SpellData = null;
            _label.text = "";
            _icon.style.backgroundImage = StyleKeyword.None;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            bool empty = IsEmpty;
            EnableInClassList(UssEmpty, empty);
            EnableInClassList(UssFilled, !empty);
            _placeholder.style.display = empty ? DisplayStyle.Flex : DisplayStyle.None;
            _icon.style.display = empty ? DisplayStyle.None : DisplayStyle.Flex;
            _label.style.display = empty ? DisplayStyle.None : DisplayStyle.Flex;
        }

    }
}
