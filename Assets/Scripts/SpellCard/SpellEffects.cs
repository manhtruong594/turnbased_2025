using TurnBasedGame.Core;
using UnityEngine;
using TurnBasedGame.Unit;

namespace TurnBasedGame.SpellCard
{
    /// <summary>
    /// Strategy Pattern - Interface cho logic xử lý hiệu ứng spell.
    /// Mỗi SpellEffectType tương ứng 1 implementation.
    /// </summary>
    public interface ISpellEffect
    {
        void Apply(SpellCardData data, PlayerID caster, UnitController target);
    }

    /// <summary>
    /// Factory tạo ISpellEffect từ SpellEffectType.
    /// </summary>
    public static class SpellEffectFactory
    {
        public static ISpellEffect Create(SpellEffectType type)
        {
            return type switch
            {
                SpellEffectType.Heal => new HealEffect(),
                SpellEffectType.Shield => new ShieldEffect(),
                SpellEffectType.Root => new RootEffect(),
                SpellEffectType.DamageBuff => new DamageBuffEffect(),
                SpellEffectType.Cleanse => new CleanseEffect(),
                _ => null
            };
        }
    }
    
    public class SpellBase: ISpellEffect
    {
        SpellCardData _baseData;
        public virtual void Apply(SpellCardData data, PlayerID caster, UnitController target)
        {
            Debug.Log($"[Spell] {data.spellName}: Hiệu ứng mặc định, không làm gì cả.");
        }
    }

    /// <summary>Hồi HP cho target.</summary>
    public class HealEffect : SpellBase
    {
        public override void Apply(SpellCardData data, PlayerID caster, UnitController target)
        {
            if (target == null || target.IsDead()) return;

            float currentPercent = target.GetHealthPercent();
            if (currentPercent >= 1f) return;

            // Heal bằng cách gây "sát thương âm" 
            target.TakeDamage(-data.effectValue);
            Debug.Log($"[Spell] {data.spellName}: Hồi {data.effectValue} HP cho {target.name}");
        }
    }

    /// <summary>Thêm Shield buff giảm sát thương nhận vào.</summary>
    public class ShieldEffect : SpellBase
    {
        public override void Apply(SpellCardData data, PlayerID caster, UnitController target)
        {
            if (target == null || target.IsDead()) return;

            var handler = target.GetComponent<BuffDebuffHandler>();
            if (handler == null) return;

            var buff = new ActiveBuff(SpellEffectType.Shield, data.effectValue, data.effectDuration, caster);
            handler.AddBuff(buff);
            Debug.Log($"[Spell] {data.spellName}: Tăng {data.effectValue} giáp cho {target.name} trong {data.effectDuration} lượt");
        }
    }

    /// <summary>Root: cấm di chuyển trong N lượt.</summary>
    public class RootEffect : SpellBase
    {
        public override void Apply(SpellCardData data, PlayerID caster, UnitController target)
        {
            if (target == null || target.IsDead()) return;

            var handler = target.GetComponent<BuffDebuffHandler>();
            if (handler == null) return;

            var buff = new ActiveBuff(SpellEffectType.Root, 0, data.effectDuration, caster);
            handler.AddBuff(buff);
            Debug.Log($"[Spell] {data.spellName}: Trói chân {target.name} trong {data.effectDuration} lượt");
        }
    }

    /// <summary>Tăng sát thương cho đơn vị đồng minh.</summary>
    public class DamageBuffEffect : SpellBase
    {
        public override void Apply(SpellCardData data, PlayerID caster, UnitController target)
        {
            if (target == null || target.IsDead()) return;

            var handler = target.GetComponent<BuffDebuffHandler>();
            if (handler == null) return;

            var buff = new ActiveBuff(SpellEffectType.DamageBuff, data.effectValue, data.effectDuration, caster);
            handler.AddBuff(buff);
            Debug.Log($"[Spell] {data.spellName}: Tăng {data.effectValue} sát thương cho {target.name} trong {data.effectDuration} lượt");
        }
    }

    /// <summary>Xóa tất cả debuff trên target.</summary>
    public class CleanseEffect : SpellBase
    {
        public override void Apply(SpellCardData data, PlayerID caster, UnitController target)
        {
            if (target == null || target.IsDead()) return;

            var handler = target.GetComponent<BuffDebuffHandler>();
            if (handler == null) return;

            handler.RemoveBuffByType(SpellEffectType.Root);
            Debug.Log($"[Spell] {data.spellName}: Thanh tẩy debuff cho {target.name}");
        }
    }
}
