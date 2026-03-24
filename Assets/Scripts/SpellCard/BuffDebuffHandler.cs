using System;
using System.Collections.Generic;
using UnityEngine;
using TurnBasedGame.Unit;
using TurnBasedGame.Core;

namespace TurnBasedGame.SpellCard
{
    /// <summary>
    /// Status effect (buff hoặc debuff) đang active trên một unit.
    /// </summary>
    public struct ActiveStatusEffect
    {
        public StatusEffectType Type { get; }
        public int Value { get; }
        public int RemainingTurns { get; private set; }
        public PlayerID SourcePlayer { get; }

        public ActiveStatusEffect(StatusEffectType type, int value, int duration, PlayerID source)
        {
            Type = type;
            Value = value;
            RemainingTurns = duration;
            SourcePlayer = source;
        }

        /// <returns>true nếu effect hết hạn</returns>
        public bool TickTurn()
        {
            RemainingTurns--;
            return RemainingTurns <= 0;
        }
    }

    /// <summary>
    /// Component quản lý tất cả buff/debuff trên một unit.
    /// Hook vào các sự kiện combat: OnTurnStart, BeforeTakeDamage, CanMove.
    /// </summary>
    public class BuffDebuffHandler : MonoBehaviour
    {
        private readonly List<ActiveStatusEffect> _activeEffects = new();
        private UnitController _owner;

        public IReadOnlyList<ActiveStatusEffect> ActiveEffects => _activeEffects;

        public event Action<ActiveStatusEffect> OnEffectAdded;
        public event Action<ActiveStatusEffect> OnEffectRemoved;

        public void Init(UnitController owner)
        {
            _owner = owner;
            _activeEffects.Clear();
        }

        public void AddEffect(ActiveStatusEffect effect)
        {
            // Cùng loại thì thay thế (refresh duration)
            RemoveByType(effect.Type);
            _activeEffects.Add(effect);
            OnEffectAdded?.Invoke(effect);
            Debug.Log($"[Effect] {_owner.name} nhận {effect.Type} ({effect.Value}) trong {effect.RemainingTurns} lượt");
        }

        /// <summary>
        /// Gọi ở đầu lượt: áp dụng hiệu ứng theo thời gian (Burn, Poison, Slow...).
        /// Phải gọi TRƯỚC TickEffects để lượt cuối vẫn có hiệu lực.
        /// </summary>
        public void ApplyTickEffects(ActiveStatusEffect effect)
        {
            switch (effect.Type)
            {
                case StatusEffectType.Burn:
                    _owner.TakeDamage(effect.Value);
                    Debug.Log($"[Tick] {_owner.name} bị Burn gây {effect.Value} sát thương");
                    break;

                case StatusEffectType.Poison:
                    // Poison: sát thương tăng dần theo số lượt đã chịu (Value = base dmg)
                    int poisonDmg = Mathf.Max(1, effect.Value * (effect.RemainingTurns == 0 ? 1 : effect.Value));
                    _owner.TakeDamage(poisonDmg);
                    Debug.Log($"[Tick] {_owner.name} bị Poison gây {poisonDmg} sát thương");
                    break;

                    // Slow / Weaken / Root: không gây damage theo thời gian, chỉ cần tồn tại
                    // Thêm case mới ở đây khi có hiệu ứng tick mới

            }
        }

        /// <summary>
        /// Gọi ở đầu mỗi lượt của unit này. Giảm duration, xóa effect hết hạn.
        /// </summary>
        public void TickEffects()
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                if (_activeEffects[i].TickTurn())
                {
                    ApplyTickEffects(_activeEffects[i]);
                    var expired = _activeEffects[i];
                    _activeEffects.RemoveAt(i);
                    OnEffectRemoved?.Invoke(expired);
                    Debug.Log($"[Effect] {_owner.name} hết hiệu ứng {expired.Type}");
                }
            }
        }

        public int GetShieldValue()
        {
            int total = 0;
            foreach (var e in _activeEffects)
                if (e.Type == StatusEffectType.Shield) total += e.Value;
            return total;
        }

        public int GetDamageBonus()
        {
            int total = 0;
            foreach (var e in _activeEffects)
                if (e.Type == StatusEffectType.DamageBuff) total += e.Value;
            return total;
        }

        public bool IsRooted() => HasEffect(StatusEffectType.Root);

        /// <summary>Lấy tất cả debuff đang active trên unit.</summary>
        public List<ActiveStatusEffect> GetDebuffs()
        {
            var result = new List<ActiveStatusEffect>();
            foreach (var e in _activeEffects)
                if (e.Type.IsDebuff()) result.Add(e);
            return result;
        }

        /// <summary>Lấy tất cả buff đang active trên unit.</summary>
        public List<ActiveStatusEffect> GetBuffs()
        {
            var result = new List<ActiveStatusEffect>();
            foreach (var e in _activeEffects)
                if (e.Type.IsBuff()) result.Add(e);
            return result;
        }

        public bool HasEffect(StatusEffectType type)
        {
            foreach (var e in _activeEffects)
                if (e.Type == type) return true;
            return false;
        }

        public void RemoveByType(StatusEffectType type)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                if (_activeEffects[i].Type == type)
                {
                    var removed = _activeEffects[i];
                    _activeEffects.RemoveAt(i);
                    OnEffectRemoved?.Invoke(removed);
                }
            }
        }

        public void ClearAll()
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var removed = _activeEffects[i];
                _activeEffects.RemoveAt(i);
                OnEffectRemoved?.Invoke(removed);
            }
        }

        public float ModifyIncomingDamage(float rawDamage)
        {
            return Mathf.Max(0, rawDamage - GetShieldValue());
        }
    }
}
