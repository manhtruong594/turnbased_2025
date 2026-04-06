# Skill System Documentation

- **Version**: 1.0 | **Updated**: 2025-12-20

---

## Kiến trúc

**Strategy Pattern + Template Method Pattern**

- **ISkill**: Interface contract chung cho mọi skill
- **SkillBase**: Abstract ScriptableObject, cung cấp template algorithm (validation pipeline)
- **Concrete Skills**: NormalAttackSkill, FireballSkill, HealSkill, ShieldSkill — mỗi skill là strategy độc lập
- **UnitSkillManager**: Context — quản lý và thực thi skills cho unit
- **SkillEventBus**: Observer — phát events (`OnSkillUsed`, `OnSkillLearned`, `OnCooldownComplete`)

---

## Các Skill có sẵn

| Skill | Type | Cost | CD | Range | AOE | Mô tả |
|-------|------|------|----|-------|-----|-------|
| **NormalAttackSkill** | Normal | 0 | 0 | 1 | — | Đánh thường, damage = baseDamage + AttackStat |
| **FireballSkill** | Active | 20 | 2 | 3 | 1 | AOE damage |
| **HealSkill** | Active | 15 | 3 | 2 | — | Hồi máu đồng minh/bản thân |
| **ShieldSkill** | Active | 25 | 4 | 2 | 2 | Tạo shield cho đồng minh trong AOE |

---

## Cách sử dụng

### Tạo Skill mới (ScriptableObject)
1. Right click Project → Create → Skills → chọn loại
2. Config thông số trong Inspector
3. Gán vào `UnitSkillManager.startingSkills`

### API chính

```csharp
var skillManager = unit.GetComponent<UnitSkillManager>();

// Sử dụng skill
skillManager.UseSkill(skill, targetPos);
skillManager.UseSkillByName("Fireball", targetPos);
skillManager.UseNormalSkill(targetPos);

// Query
skillManager.GetUsableSkills(targetPos);   // Skills dùng được tại vị trí
skillManager.GetReadySkills();              // Skills hết cooldown
skillManager.HasSkillInRange(targetPos);    // Có skill nào trong range?
```

---

## Tạo Skill mới (Code)

Kế thừa `SkillBase`, override 3 method:

```csharp
[CreateAssetMenu(fileName = "TeleportSkill", menuName = "Skills/Teleport")]
public class TeleportSkill : SkillBase
{
    protected override void ExecuteEffect(UnitMove caster, Vector3Int targetPos)
    { /* Logic chính */ }

    public override List<Vector3Int> GetValidTargets(UnitMove caster)
    { /* Trả về các tile hợp lệ */ }

    protected override bool ValidateTarget(UnitMove caster, Vector3Int targetPos)
    { /* Custom validation */ }
}
```

---

## Tích hợp với UnitAttack

- Toggle `useSkillSystem` để bật/tắt
- Click tile → tự tìm skill phù hợp → thực thi
- Ưu tiên Normal Skill, fallback logic cũ nếu không có skill

---

## Design Patterns

| Pattern | Áp dụng | Lợi ích |
|---------|---------|---------|
| **Strategy** | `ISkill` / `SkillBase` / Concrete Skills | Thêm skill mới = tạo class mới, không sửa code cũ (OCP) |
| **Template Method** | `SkillBase.CanUse()` pipeline | Validation chung: Cooldown → Range → Target → Custom |
| **Observer** | `SkillEventBus` | Decouple UI/VFX/Sound khỏi skill logic |
| **Data-Driven** | ScriptableObject | Config/share skills qua Inspector, clone cho cooldown riêng |
