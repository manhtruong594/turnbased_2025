using UnityEngine;
using UnityEngine.UIElements;
using TurnBasedGame.Unit;

namespace TurnBasedGame.UI
{
    /// <summary>
    /// Custom VisualElement hiển thị một unit card (icon, name, stats).
    /// Dùng trong Collection list và Deck slots.
    /// </summary>
    [UxmlElement]
    public partial class UnitCardElement : VisualElement
    {
        private const string UssClassName = "unit-card";
        private const string UssSelected = "unit-card--selected";
        private const string UssInDeck = "unit-card--in-deck";

        private readonly VisualElement _icon;
        private readonly Label _nameLabel;
        private readonly Label _costLabel;
        private readonly Label _hpLabel;
        private readonly Label _moveLabel;

        public UnitData Data { get; private set; }

        public UnitCardElement()
        {
            AddToClassList(UssClassName);
            AddToClassList("card");

            _icon = new VisualElement { name = "unit-icon" };
            _icon.AddToClassList("unit-card__icon");
            Add(_icon);

            var info = new VisualElement { name = "unit-info" };
            info.AddToClassList("unit-card__info");

            _nameLabel = new Label { name = "unit-name" };
            _nameLabel.AddToClassList("unit-card__name");
            info.Add(_nameLabel);

            var statsRow = new VisualElement();
            statsRow.AddToClassList("unit-card__stats");

            _costLabel = new Label();
            _costLabel.AddToClassList("unit-card__stat");
            _costLabel.AddToClassList("text-gold");
            statsRow.Add(_costLabel);

            _hpLabel = new Label();
            _hpLabel.AddToClassList("unit-card__stat");
            statsRow.Add(_hpLabel);

            _moveLabel = new Label();
            _moveLabel.AddToClassList("unit-card__stat");
            statsRow.Add(_moveLabel);

            info.Add(statsRow);
            Add(info);
        }

        /// <summary>Bind dữ liệu UnitData vào card.</summary>
        public void Bind(UnitData data)
        {
            Data = data;
            if (data == null)
            {
                Clear();
                return;
            }

            _nameLabel.text = data.unitName;
            _costLabel.text = $"⚡{data.spawnCost}";
            _hpLabel.text = $"❤{data.Health}";
            _moveLabel.text = $"👣{data.moveRange}";

            if (data.icon != null)
            {
                _icon.style.backgroundImage = new StyleBackground(data.icon);
            }
        }

        public void SetSelected(bool selected)
        {
            EnableInClassList(UssSelected, selected);
        }

        public void SetInDeck(bool inDeck)
        {
            EnableInClassList(UssInDeck, inDeck);
        }

    }
}
