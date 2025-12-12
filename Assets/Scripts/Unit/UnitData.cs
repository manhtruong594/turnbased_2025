using UnityEngine;
using TurnBasedGame.Core;

namespace TurnBasedGame.Unit
{
    /// <summary>
    /// ScriptableObject chứa cấu hình cho từng loại unit
    /// Sử dụng để tạo các template unit khác nhau
    /// </summary>
    [CreateAssetMenu(fileName = "New Unit", menuName = "TurnBased/Unit Data")]
    public class UnitData : ScriptableObject
    {
        [Header("Basic Info")]
        public string unitName = "Unit";
        [TextArea(2, 4)]
        public string description;
        public int Health = 100;

        [Range(1, 20)]
        public int spawnCost = 3;

        [Header("Movement")]
        [Range(1, 10)]
        public int moveRange = 3;
        [Range(1f, 10f)]
        public float moveSpeed = 5f;

        [Header("Attack")]
        [Range(1, 10)]
        public int attackRange = 2;
        [Range(1, 100)]
        public float attackDamage = 10;

        [Tooltip("Icon hiển thị trên UI")]
        public Sprite icon;
    }
}
