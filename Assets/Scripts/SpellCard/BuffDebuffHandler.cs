using System;
using System.Collections.Generic;
using RedBjorn.ProtoTiles.Example;
using TurnBasedGame.Core;
using UnityEngine;

namespace TurnBasedGame.SpellCard
{
    /// <summary>
    /// Buff/Debuff đang active trên một unit.
    /// Chứa thông tin hiệu ứng và thời gian còn lại.
    /// </summary>
    public class ActiveBuff
    {
        public SpellEffectType Type { get; }
        public int Value { get; }
        public int RemainingTurns { get; private set; }
        public PlayerID SourcePlayer { get; }

        public ActiveBuff(SpellEffectType type, int value, int duration, PlayerID source)
        {
            Type = type;
            Value = value;
            RemainingTurns = duration;
            SourcePlayer = source;
        }

        /// <returns>true nếu buff hết hạn</returns>
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
        private readonly List<ActiveBuff> _activeBuffs = new();
        private UnitMove _owner;

        public IReadOnlyList<ActiveBuff> ActiveBuffs => _activeBuffs;

        public event Action<ActiveBuff> OnBuffAdded;
        public event Action<ActiveBuff> OnBuffRemoved;

        public void Init(UnitMove owner)
        {
            _owner = owner;
            _activeBuffs.Clear();
        }

        public void AddBuff(ActiveBuff buff)
        {
            // Cùng loại buff thì thay thế (refresh duration)
            RemoveBuffByType(buff.Type);
            _activeBuffs.Add(buff);
            OnBuffAdded?.Invoke(buff);
            Debug.Log($"[Buff] {_owner.name} nhận {buff.Type} ({buff.Value}) trong {buff.RemainingTurns} lượt");
        }

        /// <summary>
        /// Gọi ở đầu mỗi lượt của unit này. Giảm duration, xóa buff hết hạn.
        /// </summary>
        public void TickBuffs()
        {
            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                if (_activeBuffs[i].TickTurn())
                {
                    var expired = _activeBuffs[i];
                    _activeBuffs.RemoveAt(i);
                    OnBuffRemoved?.Invoke(expired);
                    Debug.Log($"[Buff] {_owner.name} hết hiệu ứng {expired.Type}");
                }
            }
        }

        /// <summary>
        /// Tổng giáp bonus từ tất cả Shield buff.
        /// </summary>
        public int GetShieldValue()
        {
            int total = 0;
            foreach (var buff in _activeBuffs)
            {
                if (buff.Type == SpellEffectType.Shield)
                    total += buff.Value;
            }
            return total;
        }

        /// <summary>
        /// Tổng bonus damage từ DamageBuff.
        /// </summary>
        public int GetDamageBonus()
        {
            int total = 0;
            foreach (var buff in _activeBuffs)
            {
                if (buff.Type == SpellEffectType.DamageBuff)
                    total += buff.Value;
            }
            return total;
        }

        /// <summary>
        /// Unit có đang bị Root (cấm di chuyển) không.
        /// </summary>
        public bool IsRooted()
        {
            foreach (var buff in _activeBuffs)
            {
                if (buff.Type == SpellEffectType.Root)
                    return true;
            }
            return false;
        }

        public bool HasBuff(SpellEffectType type)
        {
            foreach (var buff in _activeBuffs)
            {
                if (buff.Type == type)
                    return true;
            }
            return false;
        }

        public void RemoveBuffByType(SpellEffectType type)
        {
            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                if (_activeBuffs[i].Type == type)
                {
                    var removed = _activeBuffs[i];
                    _activeBuffs.RemoveAt(i);
                    OnBuffRemoved?.Invoke(removed);
                }
            }
        }

        public void ClearAllBuffs()
        {
            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                var removed = _activeBuffs[i];
                _activeBuffs.RemoveAt(i);
                OnBuffRemoved?.Invoke(removed);
            }
        }

        /// <summary>
        /// Tính sát thương cuối cùng sau khi áp dụng Shield.
        /// </summary>
        public float ModifyIncomingDamage(float rawDamage)
        {
            int shield = GetShieldValue();
            return Mathf.Max(0, rawDamage - shield);
        }
    }
}
