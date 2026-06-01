using System.Collections;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.Skills
{
    [CreateAssetMenu(fileName = "FlameThrowerSkill", menuName = "Skills/Flame Thrower", order = 4)]
    public class FlameThrowerSkill : SkillBase
    {
        [Header("Flame Settings")]
        [SerializeField, Min(0.1f)] private float duration = 2f;
        [SerializeField, Min(0.05f)] private float tickInterval = 0.25f;
        [SerializeField, Min(0f)] private float damageMultiplierPerTick = 0.25f;
        [SerializeField, Min(0)] private int minimumDamagePerTick = 1;

        protected override IEnumerator ApplyEffectAsync(UnitController caster, Vector3Int targetPos)
        {
            float elapsed = 0f;
            var wait = new WaitForSeconds(tickInterval);

            while (elapsed < duration)
            {
                ApplyDamageTick(caster, targetPos);
                elapsed += tickInterval;
                yield return wait;
            }
        }

        private void ApplyDamageTick(UnitController caster, Vector3Int targetPos)
        {
            var target = MapManager.Instance?.GetUnitAtTile(targetPos);
            if (target == null || target.IsDead()) return;
            if (target.GetOwner() == caster.GetOwner()) return;

            int damage = CalculateTickDamage(caster);
            target.TakeDamage(damage);
        }

        private int CalculateTickDamage(UnitController caster)
        {
            int damage = Mathf.RoundToInt(caster.GetCurrentDamage() * damageMultiplierPerTick);
            return Mathf.Max(minimumDamagePerTick, damage);
        }

        private void OnValidate()
        {
            duration = Mathf.Max(0.1f, duration);
            tickInterval = Mathf.Max(0.05f, tickInterval);
            minimumDamagePerTick = Mathf.Max(0, minimumDamagePerTick);
        }
    }
}
