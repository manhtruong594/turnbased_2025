using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TurnBasedGame.Data
{
    /// <summary>
    /// ScriptableObject chứa danh mục shop — tất cả item có thể mua.
    /// Hỗ trợ filter theo type, level, và kiểm tra ownership.
    /// </summary>
    [CreateAssetMenu(fileName = "Shop Catalog", menuName = "TurnBased/Shop/Shop Catalog")]
    public class ShopCatalogSO : ScriptableObject
    {
        [SerializeField] private List<ShopItemSO> _items = new();

        public IReadOnlyList<ShopItemSO> AllItems => _items;

        /// <summary>Lọc theo loại item.</summary>
        public IEnumerable<ShopItemSO> GetByType(ShopItemType type)
        {
            return _items.Where(item => item != null && item.Type == type);
        }

        /// <summary>Lọc theo level tối đa (player level).</summary>
        public IEnumerable<ShopItemSO> GetAvailableForLevel(int playerLevel)
        {
            return _items.Where(item => item != null && item.RequiredLevel <= playerLevel);
        }

        /// <summary>Lọc kết hợp type + level.</summary>
        public IEnumerable<ShopItemSO> GetFiltered(ShopItemType type, int playerLevel)
        {
            return _items.Where(item =>
                item != null &&
                item.Type == type &&
                item.RequiredLevel <= playerLevel);
        }
    }
}
