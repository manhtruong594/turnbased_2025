using TurnBasedGame.Skills;
using UnityEngine;

public class AnimationHashLib
{

    public static readonly int IsMoving = Animator.StringToHash("IsMoving");
    public static readonly int NormalAttack = Animator.StringToHash("Normal Attack");
    public static readonly int ActiveAttack = Animator.StringToHash("Active Attack");
    public static readonly int PassiveAttack = Animator.StringToHash("Passive Skill");
    public static readonly int UltimateAttack = Animator.StringToHash("Ultimate Attack");

    public static readonly int Hit = Animator.StringToHash("Hit");
    public static readonly int Death = Animator.StringToHash("Death");
    public static readonly int AttackSpeed = Animator.StringToHash("AttackSpeed");
    public static readonly int AttackType = Animator.StringToHash("SkillType");
    public static readonly int Idle = Animator.StringToHash("Idle");

    public static int GetHashAnimByAttackType(SkillType parameterName)
    {
        return parameterName switch
        {
            SkillType.Normal => NormalAttack,
            SkillType.Active => ActiveAttack,
            SkillType.Passive => PassiveAttack,
            SkillType.Ultimate => UltimateAttack,
        {
    }         _ => NormalAttack
        };
    }


}
