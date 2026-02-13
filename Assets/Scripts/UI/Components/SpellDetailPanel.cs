using UnityEngine.UIElements;
using TurnBasedGame.Skills;
using System.Collections.Generic;

namespace TurnBasedGame.UI
{
    /// <summary>
    /// Hiển thị chi tiết spell trong Inventory: icon, stats, description, target flags.
    /// Bind vào các element có sẵn trong Inventory.uxml (section spell-detail).
    /// </summary>
    public class SpellDetailPanel
    {
        private readonly VisualElement _root;
        private readonly VisualElement _icon;
        private readonly Label _name;
        private readonly Label _type;
        private readonly Label _desc;
        private readonly Label _cd;
        private readonly Label _range;
        private readonly Label _targetFlags;
        private readonly Label _deckStatus;

        public SpellDetailPanel(VisualElement root)
        {
            _root = root;
            _icon = root.Q("spell-icon-large");
            _name = root.Q<Label>("spell-detail-name");
            _type = root.Q<Label>("spell-detail-type");
            _desc = root.Q<Label>("spell-detail-desc");
            _cd = root.Q<Label>("spell-stat-cd");
            _range = root.Q<Label>("spell-stat-range");
            _targetFlags = root.Q<Label>("spell-target-flags");
            _deckStatus = root.Q<Label>("spell-in-deck-label");
        }

        /// <summary>Bind dữ liệu SkillBase vào detail panel.</summary>
        public void Bind(SkillBase spell, bool isInDeck)
        {
            if (spell == null) return;

            if (spell.Icon != null)
                _icon.style.backgroundImage = new StyleBackground(spell.Icon);
            else
                _icon.style.backgroundImage = StyleKeyword.None;

            SetText(_name, spell.SkillName);
            SetText(_type, spell.Type.ToString());
            SetText(_desc, string.IsNullOrEmpty(spell.Description) ? "Không có mô tả." : spell.Description);
            SetText(_cd, spell.Cooldown.ToString());
            SetText(_range, spell.Range.ToString());
            SetText(_targetFlags, BuildTargetText(spell));
            SetText(_deckStatus, isInDeck ? "✔ Đang được trang bị" : "");
        }

        public void Show() => _root.style.display = DisplayStyle.Flex;
        public void Hide() => _root.style.display = DisplayStyle.None;

        private static string BuildTargetText(SkillBase spell)
        {
            var targets = new List<string>(4);
            if (spell.CanTargetEnemies) targets.Add("Địch");
            if (spell.CanTargetAllies) targets.Add("Đồng minh");
            if (spell.CanTargetSelf) targets.Add("Bản thân");
            if (spell.CanTargetEmptyTile) targets.Add("Ô trống");
            return targets.Count > 0 ? string.Join(", ", targets) : "—";
        }

        private static void SetText(Label label, string text)
        {
            if (label != null) label.text = text;
        }
    }
}
