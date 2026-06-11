using TurnBasedGame.SpellCard;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.Skills
{
    [CreateAssetMenu(fileName = "SmasherEarthquakeSkill", menuName = "Skills/Smasher Earthquake", order = 8)]
    public class SmasherEarthquakeSkill : SkillBase
    {
        [Header("Damage Settings")]
        [SerializeField, Min(0f)] private float damageMultiplier = 1.8f;
        [SerializeField, Min(0f)] private float collisionDamageMultiplier = 0.5f;

        [Header("Control Settings")]
        [SerializeField, Min(1)] private int knockbackDistance = 1;
        [SerializeField, Min(1)] private int stunDuration = 1;

        protected override void ExecuteEffect(UnitController caster, Vector3Int targetPos)
        {
            var target = MapManager.Instance?.GetUnitAtTile(targetPos);
            if (target == null || target.IsDead() || target.GetOwner() == caster.GetOwner())
                return;

            target.TakeDamage(Mathf.RoundToInt(caster.GetCurrentDamage() * damageMultiplier));
            if (target.IsDead())
                return;

            TryKnockbackOrCollide(caster, target, targetPos);
        }

        protected override bool ValidateCustomConditions(UnitController caster, Vector3Int targetPos)
        {
            Vector3Int direction = SkillAreaUtility.NormalizeDirection(targetPos - caster.currentGridPosition);
            return direction != Vector3Int.zero && MapManager.Instance?.MapEntity?.Tile(targetPos) != null;
        }

        private void TryKnockbackOrCollide(UnitController caster, UnitController target, Vector3Int targetPos)
        {
            Vector3Int direction = SkillAreaUtility.NormalizeDirection(targetPos - caster.currentGridPosition);
            var result = DisplacementUtility.TryPushUnit(target, direction, knockbackDistance);

            if (result.WasImmune || result.WasMoved)
                return;

            if (result.WasBlocked)
                ApplyCollision(caster, target);
        }

        private void ApplyCollision(UnitController caster, UnitController target)
        {
            int collisionDamage = Mathf.RoundToInt(caster.GetCurrentDamage() * collisionDamageMultiplier);
            target.TakeDamage(collisionDamage);

            if (target.IsDead())
                return;

            target.BuffHandler?.AddEffect(new ActiveStatusEffect(
                StatusEffectType.Stun,
                0,
                stunDuration,
                caster.GetOwner()));
        }
    }
}
