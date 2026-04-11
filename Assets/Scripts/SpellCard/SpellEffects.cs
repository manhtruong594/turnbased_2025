using System;
using System.Collections.Generic;
using TurnBasedGame.Core;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.SpellCard
{
    /// <summary>
    /// Strategy Pattern - Interface cho logic xử lý hiệu ứng spell.
    /// Mỗi loại spell implement interface này với params riêng.
    /// Dùng [SerializeReference] + custom PropertyDrawer để chọn trong Inspector.
    /// </summary>
    public interface ISpellEffect
    {
        void Apply(PlayerID caster, UnitController target);
    }

    public abstract class SpellBuffEffct: ISpellEffect
    {
        public abstract void Apply(PlayerID caster, UnitController target);
    }
    
    /// <summary>Hồi HP cho target.</summary>
    [Serializable]
    public class HealEffect : ISpellEffect
    {
        [Range(1, 200)] public int healAmount = 20;

        public void Apply(PlayerID caster, UnitController target)
        {
            if (target == null || target.IsDead()) return;
            if (target.GetHealthPercent() >= 1f) return;

            target.Heal(healAmount);
            Debug.Log($"[Spell] Heal: Hồi {healAmount} HP cho {target.name}");
        }
    }

    /// <summary>Thêm Shield buff giảm sát thương nhận vào.</summary>
    [Serializable]
    public class ShieldEffect : ISpellEffect
    {
        [Range(1, 100)] public int shieldValue = 20;
        [Range(1, 10)] public int duration = 2;

        public void Apply(PlayerID caster, UnitController target)
        {
            if (target == null || target.IsDead()) return;
            var handler = target.BuffHandler;
            if (handler == null) return;

            handler.AddEffect(new ActiveStatusEffect(StatusEffectType.Shield, shieldValue, duration, caster));
            Debug.Log($"[Spell] Shield: +{shieldValue} giáp cho {target.name} trong {duration} lượt");
        }
    }

    /// <summary>Tăng sát thương cho đơn vị đồng minh.</summary>
    [Serializable]
    public class DamageBuffEffect : ISpellEffect
    {
        [Range(1, 100)] public int damageBonus = 20;
        [Range(1, 10)] public int duration = 2;

        public void Apply(PlayerID caster, UnitController target)
        {
            if (target == null || target.IsDead()) return;
            var handler = target.BuffHandler;
            if (handler == null) return;

            handler.AddEffect(new ActiveStatusEffect(StatusEffectType.DamageBuff, damageBonus, duration, caster));
            Debug.Log($"[Spell] DamageBuff: +{damageBonus} damage cho {target.name} trong {duration} lượt");
        }
    }

    /// <summary>Xóa tất cả buff/debuff trên target.</summary>
    [Serializable]
    public class CleanseEffect : ISpellEffect
    {
        public void Apply(PlayerID caster, UnitController target)
        {
            if (target == null || target.IsDead()) return;
            var handler = target.BuffHandler;
            if (handler == null) return;

            handler.ClearAll();
            Debug.Log($"[Spell] Cleanse: Thanh tẩy debuff cho {target.name}");
        }
    }

    /// <summary>Root: cấm di chuyển trong N lượt.</summary>
    [Serializable]
    public class RootEffect : ISpellEffect
    {
        [Range(1, 5)] public int duration = 2;

        public void Apply(PlayerID caster, UnitController target)
        {
            if (target == null || target.IsDead()) return;
            var handler = target.BuffHandler;
            if (handler == null) return;

            handler.AddEffect(new ActiveStatusEffect(StatusEffectType.Root, 0, duration, caster));
            Debug.Log($"[Spell] Root: Cấm di chuyển {target.name} trong {duration} lượt");
        }
    }

    [Serializable]
    public class DamageEffect : ISpellEffect
    {
        public int damage = 2;

        public void Apply(PlayerID caster, UnitController target)
        {
            if (target == null || target.IsDead()) return;
            target.TakeDamage(damage);
            Debug.Log($"[Spell] Damage: Gây {damage} damage cho {target.name}");
        }
    }

    [Serializable]
    public class StunEffect : ISpellEffect
    {
        [Range(1, 5)] public int duration = 1;

        public void Apply(PlayerID caster, UnitController target)
        {
            if (target == null || target.IsDead()) return;
            var handler = target.BuffHandler;
            if (handler == null) return;

            handler.AddEffect(new ActiveStatusEffect(StatusEffectType.Stun, 0, duration, caster));
            Debug.Log($"[Spell] Stun: Choáng {target.name} trong {duration} lượt");
        }
    }

    /// <summary>
    /// Composite Pattern: Kết hợp nhiều ISpellEffect trong 1 spell card.
    /// Cho phép thiết kế spell phức tạp (Damage + Root, Heal + Cleanse...) trong Inspector mà không cần code mới.
    /// </summary>
    [Serializable]
    public class CompositeEffect : ISpellEffect
    {
        [SerializeReference, SpellEffectSelector]
        public List<ISpellEffect> effects = new();

        public void Apply(PlayerID caster, UnitController target)
        {
            foreach (var effect in effects)
                effect?.Apply(caster, target);
        }
    }
}
