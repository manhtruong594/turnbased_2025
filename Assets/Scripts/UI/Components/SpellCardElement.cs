using UnityEngine.UIElements;
using TurnBasedGame.Skills;
using TurnBasedGame.SpellCard;

namespace TurnBasedGame.UI
{
    /// <summary>
    /// Custom VisualElement hiển thị một spell card (icon, name, cooldown, range).
    /// Dùng trong Collection list và Selected Spells slots.
    /// </summary>
    [UxmlElement]
    public partial class SpellCardElement : VisualElement
    {
        private const string UssClassName = "spell-card";
        private const string UssSelected = "spell-card--selected";
        private const string UssInDeck = "spell-card--in-deck";

        private readonly VisualElement _icon;
        private readonly Label _nameLabel;
        private readonly Label _typeLabel;
        private readonly Label _mpCostLabel;
        private readonly Label _rangeLabel;

        public SpellCardData Data { get; private set; }

        public SpellCardElement()
        {
            AddToClassList(UssClassName);
            AddToClassList("card");

            _icon = new VisualElement { name = "spell-icon" };
            _icon.AddToClassList("spell-card__icon");
            Add(_icon);

            var info = new VisualElement { name = "spell-info" };
            info.AddToClassList("spell-card__info");

            _nameLabel = new Label { name = "spell-name" };
            _nameLabel.AddToClassList("spell-card__name");
            info.Add(_nameLabel);

            _typeLabel = new Label();
            _typeLabel.AddToClassList("spell-card__type");
            _typeLabel.AddToClassList("text-dim");
            info.Add(_typeLabel);

            var statsRow = new VisualElement();
            statsRow.AddToClassList("spell-card__stats");

            _mpCostLabel = new Label();
            _mpCostLabel.AddToClassList("spell-card__stat");
            statsRow.Add(_mpCostLabel);

            _rangeLabel = new Label();
            _rangeLabel.AddToClassList("spell-card__stat");
            statsRow.Add(_rangeLabel);

            info.Add(statsRow);
            Add(info);
        }

        /// <summary>Bind dữ liệu SpellCardData vào card.</summary>
        public void Bind(SpellCardData data)
        {
            Data = data;
            if (data == null) return;

            _nameLabel.text = data.spellName;
            _typeLabel.text = data.targetType.ToString();
            _mpCostLabel.text = $"🔄{data.mpCost}";
            _rangeLabel.text = $"🎯{data.range}";

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
