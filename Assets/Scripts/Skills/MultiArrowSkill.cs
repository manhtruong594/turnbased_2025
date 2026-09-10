using System.Collections;
using TurnBasedGame.Skills;
using TurnBasedGame.SpellCard;
using TurnBasedGame.Unit;
using UnityEngine;

[CreateAssetMenu(fileName = "MultiArrowSkill", menuName = "Skills/Multi Arrow", order = 3)]
public class MultiArrowSkill : SkillBase
{
    private int _currentArrowCount = 0;

    [Header("Arrow Settings")]
    [Min(1)]
    public int MaxArrowCount = 3;

    [Header("Damage Settings")]
    [SerializeField, Min(0f)] private float firstArrowDamageMultiplier = 1f;
    [SerializeField, Min(0f)] private float extraArrowMinDamageMultiplier = 0.4f;
    [SerializeField, Min(0f)] private float extraArrowMaxDamageMultiplier = 0.7f;

    [Header("Effect Settings")]
    [SerializeField, Range(0f, 1f)] private float extraArrowEffectChance = 0.25f;

    [SerializeReference, SpellEffectSelector]
    public ISpellEffect CurrentArrowEffect;

    protected override void ExecuteEffect(UnitController caster, Vector3Int targetPos)
    {
        var target = MapManager.Instance?.GetUnitAtTile(targetPos);

        int arrowCount = Mathf.Max(1, MaxArrowCount);
        for (_currentArrowCount = 1; _currentArrowCount <= arrowCount; _currentArrowCount++)
        {
            if (target.IsDead()) break;

            int damage = CalculateArrowDamage(caster, _currentArrowCount);
            target.TakeDamage(damage);

            if (_currentArrowCount > 1)
                TryApplyExtraArrowEffect(caster, target);
        }

        _currentArrowCount = 0;
    }

    // trong anim sẽ gọi 2 lần ApplyEffectAsync
    protected override IEnumerator ApplyEffectAsync(UnitController caster, Vector3Int targetPos)
    {
        var target = MapManager.Instance?.GetUnitAtTile(targetPos);
        if (target == null) yield break;
        if (target.IsDead()) yield break;
        if (_currentArrowCount == 0)
        {
            _currentArrowCount++;
            int damage = CalculateArrowDamage(caster, _currentArrowCount);
            target.TakeDamage(damage);

            yield break;
        }
        else
        {
            _currentArrowCount++;
            int damage = CalculateArrowDamage(caster, _currentArrowCount);
            target.TakeDamage(damage);

            TryApplyExtraArrowEffect(caster, target);
        }
        if (_currentArrowCount >= MaxArrowCount)
        {
            _currentArrowCount = 0;
            _isExecuting = false;
        }
        yield break;
    }

    private int CalculateArrowDamage(UnitController caster, int arrowIndex)
    {
        float multiplier = arrowIndex == 1
            ? firstArrowDamageMultiplier
            : TurnBasedGame.Command.LocalMatchAuthority.Random.Range(extraArrowMinDamageMultiplier, extraArrowMaxDamageMultiplier);

        return Mathf.RoundToInt(caster.GetCurrentDamage() * multiplier);
    }

    private void TryApplyExtraArrowEffect(UnitController caster, UnitController target)
    {
        if (CurrentArrowEffect == null) return;
        if (TurnBasedGame.Command.LocalMatchAuthority.Random.Value() > extraArrowEffectChance) return;

        CurrentArrowEffect.Apply(caster.GetOwner(), target);
    }

    private void OnValidate()
    {
        MaxArrowCount = Mathf.Max(1, MaxArrowCount);
        if (extraArrowMaxDamageMultiplier < extraArrowMinDamageMultiplier)
            extraArrowMaxDamageMultiplier = extraArrowMinDamageMultiplier;
    }
}
