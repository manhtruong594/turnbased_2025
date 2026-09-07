using UnityEngine.UIElements;
using TurnBasedGame.Skills;
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
        private readonly VisualElement _skillsList;
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
            _skillsList = root.Q("unit-skills-list");
            _deckStatus = root.Q<Label>("unit-in-deck-label");
        }

        /// <summary>Bind dữ liệu và danh sách skill của unit vào detail panel.</summary>
        public void Bind(UnitController unit, bool isInDeck)
        {
            if (unit == null || unit.UnitData == null) return;

            var unitData = unit.UnitData;

            if (unitData.icon != null)
                _icon.style.backgroundImage = new StyleBackground(unitData.icon);
            else
                _icon.style.backgroundImage = StyleKeyword.None;

            SetText(_name, unitData.unitName);
            SetText(_desc, string.IsNullOrEmpty(unitData.description) ? "Không có mô tả." : unitData.description);
            SetText(_hp, unitData.Health.ToString());
            SetText(_cost, unitData.spawnCost.ToString());
            SetText(_move, unitData.moveRange.ToString());
            BindSkills(unitData);
            SetText(_deckStatus, isInDeck ? "✔ Đang trong đội hình" : "");
        }

        private void BindSkills(UnitData unitData)
        {
            if (_skillsList == null) return;

            _skillsList.Clear();
            var skills = unitData.StartingSkills;
            if (skills == null || skills.Count == 0)
            {
                var emptyLabel = new Label("Không có kỹ năng.");
                emptyLabel.AddToClassList("inv-skill-empty");
                _skillsList.Add(emptyLabel);
                return;
            }

            bool hasSkill = false;
            for (int i = 0; i < skills.Count; i++)
            {
                SkillBase skill = skills[i];
                if (skill == null) continue;
                hasSkill = true;

                var entry = new VisualElement();
                entry.AddToClassList("inv-skill-entry");

                var nameLabel = new Label(skill.SkillName);
                nameLabel.AddToClassList("inv-skill-name");
                entry.Add(nameLabel);

                var descriptionLabel = new Label(
                    string.IsNullOrEmpty(skill.Description) ? "Không có mô tả." : skill.Description);
                descriptionLabel.AddToClassList("inv-skill-description");
                entry.Add(descriptionLabel);

                _skillsList.Add(entry);
            }

            if (!hasSkill)
            {
                var emptyLabel = new Label("Không có kỹ năng.");
                emptyLabel.AddToClassList("inv-skill-empty");
                _skillsList.Add(emptyLabel);
            }
        }

        public void Show() => _root.style.display = DisplayStyle.Flex;
        public void Hide() => _root.style.display = DisplayStyle.None;

        private static void SetText(Label label, string text)
        {
            if (label != null) label.text = text;
        }
    }
}
