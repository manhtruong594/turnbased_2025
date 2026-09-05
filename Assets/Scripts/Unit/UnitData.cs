using UnityEngine;
using TurnBasedGame.Core;
using TurnBasedGame.EditorSupport;

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
        public int BaseDamage = 10;
        
        [Range(1, 20)]
        public int spawnCost = 3;

        [Header("Movement")]
        [Range(1, 10)]
        public int moveRange = 3;
        [Range(1f, 10f)]
        public float moveSpeed = 5f;

        [Tooltip("Icon hiển thị trên UI")]
        [SpritePreview]
        public Sprite icon;
    }
}
