using TurnBasedGame.Core;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.SpellCard
{
    /// <summary>
    /// ScriptableObject định nghĩa data cho một Spell Card.
    /// Data-driven: tạo spell mới chỉ cần tạo asset, không sửa code.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSpellCard", menuName = "TurnBased/Spell Card")]
    public class SpellCardData : ScriptableObject
    {
        [Header("Basic Info")]
        public string spellName = "Unnamed Spell";
        [TextArea(2, 4)] public string description;
        public Sprite icon;

        [Header("Cost & Restrictions")]
        [Range(1, 10)] public int mpCost = 2;
        [Tooltip("Spell bị hủy sau khi dùng (true = 1 lần duy nhất)")]
        public bool consumeOnUse = true;

        [Header("Targeting")]
        public SpellTargetType targetType = SpellTargetType.SingleAlly;
        [Range(0, 10)] public int range = 3;

        [Header("Effect")]
        [SerializeReference, SpellEffectSelector]
        public ISpellEffect spellEffect;

        [Header("Visual")]
        public GameObject castVfxPrefab;
        public GameObject impactVfxPrefab;

        public void Cast(PlayerID caster, UnitController target)
        {
            if (spellEffect == null)
            {
                Debug.LogWarning($"[SpellCard] {spellName} không có hiệu ứng để cast.");
                return;
            }

            spellEffect.Apply(caster, target);
        }
    }

    public enum SpellTargetType
    {
        SingleAlly,
        SingleEnemy,
        Self,
        AllAllies,
        AllEnemies,
        AnyUnit
    }

    public enum StatusEffectType
    {
        None = 0,
        // Buffs
        HealOverTime = 1,
        Shield = 2,
        DamageBuff = 3,
        CleanseOverTime = 4,
        BloodRage = 5,
        StanceGuard = 6,
        ShadowStep = 7,
        // Debuffs
        Burn = 100,
        Poison = 101,
        Slow = 102,
        Weaken = 103,
        Root = 104,
        Stun = 105,
        Freeze = 106,
        Bleed = 107,
        GuardBreak = 108,
        HealBan = 109,
    }

    public static class StatusEffectTypeExtensions
    {
        public static bool IsDebuff(this StatusEffectType type) => type switch
        {
            StatusEffectType.Burn    => true,
            StatusEffectType.Poison  => true,
            StatusEffectType.Slow    => true,
            StatusEffectType.Weaken  => true,
            StatusEffectType.Root    => true,
            StatusEffectType.Stun    => true,
            StatusEffectType.Freeze  => true,
            StatusEffectType.Bleed   => true,
            StatusEffectType.GuardBreak => true,
            StatusEffectType.HealBan => true,
            _                        => false,
        };

        public static bool IsBuff(this StatusEffectType type) => !type.IsDebuff() && type != StatusEffectType.None;
    }
}
