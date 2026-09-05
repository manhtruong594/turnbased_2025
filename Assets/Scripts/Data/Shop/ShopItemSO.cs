using UnityEngine;
using TurnBasedGame.EditorSupport;

namespace TurnBasedGame.Data
{
    public enum ShopItemType
    {
        Unit,
        Spell,
        Cosmetic
    }

    /// <summary>
    /// ScriptableObject đại diện 1 item trong Shop.
    /// Chứa metadata hiển thị + ref đến data thật (UnitData hoặc SkillBase).
    /// </summary>
    [CreateAssetMenu(fileName = "New Shop Item", menuName = "TurnBased/Shop/Shop Item")]
    public class ShopItemSO : ScriptableObject
    {
        [Header("Display Info")]
        [SerializeField] private string _itemName = "Unnamed Item";
        [SerializeField, TextArea(2, 4)] private string _description;
        [SerializeField, SpritePreview] private Sprite _icon;

        [Header("Pricing")]
        [SerializeField, Min(0)] private int _price = 50;
        [Tooltip("Level tối thiểu để mua")]
        [SerializeField, Min(1)] private int _requiredLevel = 1;

        [Header("Item Reference")]
        [SerializeField] private ShopItemType _type;
        [Tooltip("Kéo UnitData hoặc SkillBase SO vào đây")]
        [SerializeField] private ScriptableObject _itemRef;

        // ─── Properties ───

        public string ItemName => _itemName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public int Price => _price;
        public int RequiredLevel => _requiredLevel;
        public ShopItemType Type => _type;
        public ScriptableObject ItemRef => _itemRef;
    }
}
