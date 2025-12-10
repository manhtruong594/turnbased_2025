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
        [Tooltip("Tên hiển thị của unit")]
        public string unitName = "Unit";
        
        [Tooltip("Mô tả về unit")]
        [TextArea(2, 4)]
        public string description;

        [Header("Cost")]
        [Tooltip("Chi phí MP để spawn unit này")]
        [Range(1, 20)]
        public int spawnCost = 3;

        [Header("Movement")]
        [Tooltip("Phạm vi di chuyển của unit (số ô)")]
        [Range(1, 10)]
        public float moveRange = 3f;
        
        [Tooltip("Tốc độ di chuyển")]
        [Range(1f, 10f)]
        public float moveSpeed = 5f;

        [Tooltip("Icon hiển thị trên UI")]
        public Sprite icon;
    }
}
