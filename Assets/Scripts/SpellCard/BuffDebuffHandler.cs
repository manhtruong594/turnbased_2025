using System;
using System.Collections.Generic;
using UnityEngine;
using TurnBasedGame.Unit;
using TurnBasedGame.Core;
using TurnBasedGame.ObjectPool;

namespace TurnBasedGame.SpellCard
{
    /// <summary>
    /// Status effect (buff hoặc debuff) đang active trên một unit.
    /// </summary>
    public struct ActiveStatusEffect
    {
        public StatusEffectType Type { get; }
        public int Value { get; }
        public int InitialDuration { get; }
        public int RemainingTurns { get; private set; }
        public PlayerID SourcePlayer { get; }

        public ActiveStatusEffect(StatusEffectType type, int value, int duration, PlayerID source)
        {
            Type = type;
            Value = value;
            InitialDuration = Mathf.Max(1, duration);
            RemainingTurns = InitialDuration;
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
        [SerializeField] private BuffIconDisplay _iconDisplay;

        public IReadOnlyList<ActiveStatusEffect> ActiveEffects => _activeEffects;

        public event Action<ActiveStatusEffect> OnEffectAdded;
        public event Action<ActiveStatusEffect> OnEffectRemoved;
        List<BuffEffect> activeVFX = new List<BuffEffect>();

        public void Init(UnitController owner)
        {
            _owner = owner;
            _activeEffects.Clear();
            if (_iconDisplay != null)
            {
                OnEffectAdded -= _iconDisplay.OnEffectAdded;
                OnEffectRemoved -= _iconDisplay.OnEffectRemoved;
                OnEffectAdded += _iconDisplay.OnEffectAdded;
                OnEffectRemoved += _iconDisplay.OnEffectRemoved;
            }
        }

        public void AddEffect(ActiveStatusEffect effect)
        {
            // Cùng loại thì thay thế (refresh duration)
            RemoveByType(effect.Type);
            _activeEffects.Add(effect);
            OnEffectAdded?.Invoke(effect);
            SpawnVfx(effect);
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
                case StatusEffectType.HealOverTime:
                    _owner.Heal(effect.Value);
                    Debug.Log($"[Tick] {_owner.name} receives {effect.Value} healing from HealOverTime");
                    break;

                case StatusEffectType.Burn:
                    _owner.TakeDamage(effect.Value, false);
                    Debug.Log($"[Tick] {_owner.name} bị Burn gây {effect.Value} sát thương");
                    break;

                case StatusEffectType.Poison:
                    int elapsedTurns = effect.InitialDuration - effect.RemainingTurns + 1;
                    int poisonDmg = Mathf.Max(1, effect.Value * elapsedTurns);
                    _owner.TakeDamage(poisonDmg, false);
                    Debug.Log($"[Tick] {_owner.name} bị Poison gây {poisonDmg} sát thương");
                    break;

                case StatusEffectType.Bleed:
                    _owner.TakeDamage(effect.Value, false);
                    Debug.Log($"[Tick] {_owner.name} bị Bleed gây {effect.Value} sát thương");
                    break;

                    // Slow / Weaken / Root: không gây damage theo thời gian, chỉ cần tồn tại
                    // Thêm case mới ở đây khi có hiệu ứng tick mới

            }
        }

        /// <summary>
        /// Gọi ở đầu mỗi lượt của unit này. Áp dụng tick và giảm duration.
        /// Effect hết duration vẫn tồn tại hết lượt hiện tại và được xóa khi unit hoàn tất action.
        /// </summary>
        public void TickEffects()
        {
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                if (_owner.IsDead())
                    break;

                var effect = _activeEffects[i];
                ApplyTickEffects(effect);
                effect.TickTurn();
                _activeEffects[i] = effect;
            }
        }

        public void RemoveExpiredEffects()
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                if (effect.RemainingTurns > 0)
                    continue;

                _activeEffects.RemoveAt(i);
                OnEffectRemoved?.Invoke(effect);
                DespawnVfx(effect.Type);
                Debug.Log($"[Effect] {_owner.name} hết hiệu ứng {effect.Type}");
            }
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
                    DespawnVfx(removed.Type);
                }
            }
        }

        private void SpawnVfx(ActiveStatusEffect effect)
        {
            var vfxSrc = EffectManager.Instance.GetBuffEffect(effect.Type);
            if (vfxSrc == null) return;

            var vfx = ObjectPoolManager.Instance.Spawn<BuffEffect>(effect.Type.ToString(), _owner.transform);
            activeVFX.Add(vfx);
            vfx.effectType = effect.Type;
            vfx.transform.localPosition = Vector3.zero;
        }

        private void DespawnVfx(StatusEffectType type)
        {
            for (int i = activeVFX.Count - 1; i >= 0; i--)
            {
                if (activeVFX[i].effectType == type)
                {
                    ObjectPoolManager.Instance.Despawn(activeVFX[i]);
                    activeVFX.RemoveAt(i);
                }
            }
        }

        #region  SUPPORTTING METHODS

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

        public int GetDamagePercentBonus()
        {
            int total = 0;
            foreach (var e in _activeEffects)
                if (e.Type == StatusEffectType.BloodRage) total += e.Value;
            return total;
        }

        public int GetMoveBonus()
        {
            int total = 0;
            foreach (var e in _activeEffects)
                if (e.Type == StatusEffectType.BloodRage) total += 1;
            return total;
        }

        public int GetMovePercentPenalty()
        {
            int total = 0;
            foreach (var effect in _activeEffects)
            {
                if (effect.Type == StatusEffectType.Slow)
                    total += Mathf.Max(0, effect.Value);
            }

            return Mathf.Clamp(total, 0, 100);
        }

        public int GetDamagePercentPenalty()
        {
            int total = 0;
            foreach (var effect in _activeEffects)
                if (effect.Type == StatusEffectType.Weaken)
                    total += Mathf.Max(0, effect.Value);
            return Mathf.Clamp(total, 0, 100);
        }

        public bool CanReceiveHealing() => HasEffect(StatusEffectType.HealBan) ;

        public bool IsUntargetableDirectly() => HasEffect(StatusEffectType.ShadowStep);

        public bool IsImmuneToDisplacement() => HasEffect(StatusEffectType.StanceGuard);

        public bool IsRooted() => HasEffect(StatusEffectType.Root) || PreventsAction();

        public bool PreventsAction() => HasEffect(StatusEffectType.Stun, StatusEffectType.Freeze);

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

        /// <summary>Trả về true khi có ít nhất một effect trong danh sách truyền vào.</summary>
        public bool HasEffect(params StatusEffectType[] types)
        {
            if (types == null || types.Length == 0)
                return false;

            foreach (var e in _activeEffects)
            {
                foreach (var type in types)
                    if (e.Type == type) return true;
            }

            return false;
        }

        public void ClearAll()
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var removed = _activeEffects[i];
                _activeEffects.RemoveAt(i);
                OnEffectRemoved?.Invoke(removed);
                DespawnVfx(removed.Type);
            }
        }

        public void RemoveDebuffs()
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                if (!_activeEffects[i].Type.IsDebuff())
                    continue;

                var removed = _activeEffects[i];
                _activeEffects.RemoveAt(i);
                OnEffectRemoved?.Invoke(removed);
                DespawnVfx(removed.Type);
            }
        }

        public float ModifyIncomingDamage(float rawDamage, bool canBreakFreeze)
        {
            float modifiedDamage = rawDamage;
            ActiveStatusEffect freeze = default;
            bool shouldBreakFreeze = false;

            foreach (var effect in _activeEffects)
            {
                if (effect.Type == StatusEffectType.GuardBreak)
                    modifiedDamage *= 1f + effect.Value / 100f;
                else if (effect.Type == StatusEffectType.StanceGuard)
                    modifiedDamage *= Mathf.Max(0f, 1f - effect.Value / 100f);
                else if (canBreakFreeze && effect.Type == StatusEffectType.Freeze)
                {
                    freeze = effect;
                    shouldBreakFreeze = true;
                    modifiedDamage *= 1.2f;
                }
            }

            if (shouldBreakFreeze)
            {
                RemoveByType(StatusEffectType.Freeze);
                if (freeze.RemainingTurns > 0)
                {
                    AddEffect(new ActiveStatusEffect(
                        StatusEffectType.Slow,
                        20,
                        freeze.RemainingTurns,
                        freeze.SourcePlayer));
                }
            }

            return Mathf.Max(0, modifiedDamage - GetShieldValue());
        }

        #endregion
    }
}
