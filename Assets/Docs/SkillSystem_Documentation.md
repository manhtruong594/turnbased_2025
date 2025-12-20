# Skill System Documentation - Strategy Pattern

## Tổng quan

Hệ thống skill sử dụng **Strategy Pattern** kết hợp với **Template Method Pattern** để tạo ra một kiến trúc linh hoạt, dễ mở rộng và bảo trì.

## Kiến trúc

### 1. Strategy Pattern
- **ISkill**: Interface định nghĩa contract chung cho mọi skill
- **SkillBase**: Abstract class implement ISkill, cung cấp template algorithm
- **Concrete Skills**: NormalAttackSkill, FireballSkill, HealSkill, ShieldSkill - mỗi skill là một strategy độc lập

### 2. Context
- **UnitSkillManager**: Context trong Strategy Pattern, quản lý và thực thi skills

### 3. Observer Pattern
- **SkillEventBus**: Phát các event liên quan đến skill (used, learned, cooldown complete)

## Các Skill có sẵn

### 1. NormalAttackSkill (Normal)
- **Type**: Normal
- **Mana Cost**: 0
- **Cooldown**: 0
- **Range**: 1
- **Description**: Đánh thường, gây damage = baseDamage + AttackStat

### 2. FireballSkill (Active)
- **Type**: Active
- **Mana Cost**: 20
- **Cooldown**: 2 turns
- **Range**: 3
- **AOE Radius**: 1
- **Description**: Bắn fireball gây damage AOE

### 3. HealSkill (Active)
- **Type**: Active
- **Mana Cost**: 15
- **Cooldown**: 3 turns
- **Range**: 2
- **Description**: Hồi máu cho đồng minh hoặc bản thân

### 4. ShieldSkill (Active)
- **Type**: Active
- **Mana Cost**: 25
- **Cooldown**: 4 turns
- **Range**: 2
- **AOE Radius**: 2
- **Description**: Tạo shield cho đồng minh trong vùng AOE

## Cách sử dụng

### Tạo Skill mới (ScriptableObject)

1. Right click trong Project → Create → Skills → chọn loại skill
2. Đặt tên và config các thông số
3. Gán skill vào `UnitSkillManager.startingSkills` trong Inspector

### Thêm skill vào unit runtime

```csharp
// Lấy UnitSkillManager
var skillManager = unit.GetComponent<UnitSkillManager>();

// Load skill từ Resources
var fireballData = Resources.Load<SkillBase>("Skills/Fireball");

// Thêm skill
skillManager.AddSkill(fireballData);
```

### Sử dụng skill

```csharp
// Cách 1: Dùng skill object
ISkill skill = skillManager.NormalSkill;
Vector3Int targetPos = targetTile.Position;
skillManager.UseSkill(skill, targetPos);

// Cách 2: Dùng skill name
skillManager.UseSkillByName("Fireball", targetPos);

// Cách 3: Dùng Normal Skill
skillManager.UseNormalSkill(targetPos);
```

### Query skills

```csharp
// Lấy skills có thể dùng tại vị trí
var usableSkills = skillManager.GetUsableSkills(targetPos);

// Lấy skills đã sẵn sàng (cooldown = 0)
var readySkills = skillManager.GetReadySkills();

// Kiểm tra có skill nào dùng được tại target không
bool canUse = skillManager.HasSkillInRange(targetPos);

// Lấy tất cả target hợp lệ của mọi skill
var allTargets = skillManager.GetAllValidTargets();
```

### Listen skill events

```csharp
void Start()
{
    SkillEventBus.Instance.OnSkillUsed += HandleSkillUsed;
    SkillEventBus.Instance.OnSkillLearned += HandleSkillLearned;
    SkillEventBus.Instance.OnCooldownComplete += HandleCooldownComplete;
}

void HandleSkillUsed(ISkill skill, UnitMove caster, Vector3Int targetPos)
{
    Debug.Log($"{caster.name} used {skill.SkillName}");
    // Update UI, spawn VFX, etc.
}

void OnDestroy()
{
    if (SkillEventBus.Instance != null)
    {
        SkillEventBus.Instance.OnSkillUsed -= HandleSkillUsed;
        // Unsubscribe other events...
    }
}
```

## Tạo Skill mới (Code)

### Ví dụ: Teleport Skill

```csharp
using UnityEngine;
using System.Collections.Generic;
using TurnBasedGame.Unit;

namespace TurnBasedGame.Skills
{
    [CreateAssetMenu(fileName = "TeleportSkill", menuName = "Skills/Teleport", order = 10)]
    public class TeleportSkill : SkillBase
    {
        [Header("Teleport Settings")]
        [SerializeField] private GameObject teleportVFX;

        protected override void ExecuteEffect(UnitMove caster, Vector3Int targetPos)
        {
            // Spawn VFX tại vị trí cũ
            if (teleportVFX != null)
            {
                Instantiate(teleportVFX, caster.transform.position, Quaternion.identity);
            }

            // Teleport unit
            var map = GetMap(caster);
            var targetWorldPos = map.WorldPosition(targetPos);
            caster.transform.position = targetWorldPos;
            caster.currentGridPosition = targetPos;

            // Spawn VFX tại vị trí mới
            if (teleportVFX != null)
            {
                Instantiate(teleportVFX, targetWorldPos, Quaternion.identity);
            }

            Debug.Log($"{caster.name} teleport đến {targetPos}");
        }

        public override List<Vector3Int> GetValidTargets(UnitMove caster)
        {
            var targets = new List<Vector3Int>();
            var map = GetMap(caster);
            var myTile = map.Tile(caster.transform.position);
            if (myTile == null) return targets;

            var tilesInRange = map.WalkableTiles(myTile.Position, range);
            
            foreach (var tile in tilesInRange)
            {
                // Chỉ teleport vào ô trống
                var unitAtTile = MapManager.Instance?.GetUnitAtTile(tile.Position);
                if (unitAtTile == null)
                {
                    targets.Add(tile.Position);
                }
            }

            return targets;
        }

        public override List<Vector3Int> GetAffectedTiles(Vector3Int targetPos)
        {
            return new List<Vector3Int> { targetPos };
        }

        protected override bool ValidateTarget(UnitMove caster, Vector3Int targetPos)
        {
            // Teleport vào ô trống
            var unitAtTile = MapManager.Instance?.GetUnitAtTile(targetPos);
            return unitAtTile == null;
        }
    }
}
```

## Ưu điểm của Strategy Pattern

### 1. Open/Closed Principle
- Mở cho mở rộng: Thêm skill mới chỉ cần tạo class mới
- Đóng cho sửa đổi: Không cần sửa code cũ

### 2. Single Responsibility
- Mỗi skill class chỉ quan tâm logic riêng của nó
- SkillManager quản lý skills
- SkillEventBus xử lý events

### 3. Dễ test
- Mỗi skill độc lập, dễ unit test
- Mock ISkill interface để test UnitSkillManager

### 4. Flexibility
- Runtime thêm/xóa skills
- Swap strategies dễ dàng
- Data-driven: Config skills qua ScriptableObject

### 5. Reusability
- Skills là ScriptableObject → share giữa nhiều units
- Clone skill → mỗi unit có instance riêng

## Tích hợp với UnitAttack

File [`UnitAttack.cs`](e:\UnityProject\My Turnbased\turnbased_2025\Assets\Scripts\Unit\UnitAttack.cs) đã được tích hợp:

- Toggle `useSkillSystem` để bật/tắt skill system
- Khi click vào tile, tự động tìm skill phù hợp và thực thi
- Ưu tiên dùng Normal Skill
- Fallback về logic cũ nếu không có skill

## Best Practices

1. **Luôn validate** trước khi execute skill
2. **Sử dụng events** để decouple systems (UI, VFX, Sound)
3. **Clone skills** từ ScriptableObject để mỗi unit có cooldown riêng
4. **Custom validation** qua override `ValidateCustomConditions()`
5. **AOE skills** implement `GetAffectedTiles()` đúng cách
6. **Cooldown management** tự động qua UnitSkillManager.OnTurnEnd()

## Mở rộng thêm

### Passive Skills
```csharp
public class PassiveRegenerationSkill : SkillBase
{
    [SerializeField] private int healPerTurn = 5;

    public void OnTurnStart(UnitMove owner)
    {
        if (Type == SkillType.Passive)
        {
            owner.RuntimeStats.CurrentHealth += healPerTurn;
        }
    }
}
```

### Skill Combo System
```csharp
public class SkillComboSystem
{
    private List<ISkill> comboChain = new List<ISkill>();
    
    public void AddToCombo(ISkill skill)
    {
        comboChain.Add(skill);
        CheckCombo();
    }
    
    private void CheckCombo()
    {
        // Check for specific skill sequences
        // Trigger bonus effects
    }
}
```

### Skill Tree
```csharp
[System.Serializable]
public class SkillNode
{
    public SkillBase skill;
    public List<SkillNode> prerequisites;
    public int requiredLevel;
    
    public bool CanUnlock(UnitMove unit)
    {
        return unit.Level >= requiredLevel && 
               prerequisites.All(p => p.IsUnlocked);
    }
}
```

## Troubleshooting

### Skill không thể dùng
- Kiểm tra cooldown
- Kiểm tra mana
- Kiểm tra range
- Debug `CanUse()` để xem validation nào fail

### Skill không gây damage
- Kiểm tra target validation
- Verify `ExecuteEffect()` được gọi
- Check MapManager.Instance không null

### VFX không spawn
- Kiểm tra prefab đã assign
- Verify position đúng
- Check GameObject hierarchy

---

**Tác giả**: GitHub Copilot  
**Ngày tạo**: 20/12/2025  
**Version**: 1.0
