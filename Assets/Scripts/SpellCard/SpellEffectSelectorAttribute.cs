using UnityEngine;

namespace TurnBasedGame.SpellCard
{
    /// <summary>
    /// Attribute đánh dấu field [SerializeReference] ISpellEffect
    /// để PropertyDrawer hiển thị dropdown chọn loại effect trong Inspector.
    /// </summary>
    public class SpellEffectSelectorAttribute : PropertyAttribute { }
}
