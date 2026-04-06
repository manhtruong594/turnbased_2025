# Spell Card System Documentation

- **Version**: 2.0 | **Updated**: 2026-03-24

---

## Kiến trúc hệ thống

```
GameMediator (OnSpellCardUsed, OnHandChanged)
       │
       ▼
SpellCardManager (Singleton) ─── runtime logic: select, confirm, validate, execute
       │
  ┌────┴─────┬──────────────┬──────────────┐
  ▼          ▼              ▼              ▼
SpellCardData  ISpellEffect    SpellCardPanel   SpellCardConfirmUI
(SO)         (SerializeRef)   (UI Hand)        (Confirm/Cancel)
              │               SpellCardBase
       ┌──────┼──────┬──────────┬──────────┬──────────┐
       ▼      ▼      ▼          ▼          ▼          ▼
     Heal   Shield  DamageBuff Cleanse    Root     Damage
              │
       BuffDebuffHandler (Component on Unit) → BuffIconDisplay
```

| Component | Pattern | Trách nhiệm |
|-----------|---------|-------------|
| **SpellCardData** | ScriptableObject | Data spell (tên, cost, target, effect reference) |
| **SpellCardManager** | Singleton | Chọn card, confirm, validate target, trừ MP, kích hoạt effect |
| **ISpellEffect** | Strategy (SerializeReference) | Interface cho hiệu ứng spell, gán trực tiếp trên SO |
| **SpellCardConfirmUI** | MonoBehaviour | UI xác nhận: card bay lên giữa màn hình + nút Confirm/Cancel |
| **BuffDebuffHandler** | Component | Quản lý status effects (buff/debuff) trên từng unit |
| **SpellCardPanel / SpellCardBase** | UI | Hiển thị danh sách spell card + button cho từng card |
| **BuffIconDisplay** | Observer | Hiển thị icon buff trên đầu unit |

---

## Data Layer

### SpellCardData (ScriptableObject)

```csharp
[CreateAssetMenu(fileName = "NewSpellCard", menuName = "TurnBased/Spell Card")]
public class SpellCardData : ScriptableObject
{
    public string spellName;
    public string description;
    public Sprite icon;
    public int mpCost = 2;
    public bool consumeOnUse = true;           // card bị xóa sau khi dùng
    public SpellTargetType targetType;
    public int range = 3;

    [SerializeReference, SpellEffectSelector]
    public ISpellEffect spellEffect;           // Polymorphic — chọn effect trong Inspector dropdown

    public GameObject castVfxPrefab, impactVfxPrefab;

    public void Cast(PlayerID caster, UnitController target)
        => spellEffect?.Apply(caster, target);
}
```

### Enums

```csharp
public enum SpellTargetType
{ SingleAlly, SingleEnemy, Self, AllAllies, AllEnemies, AnyUnit }

public enum StatusEffectType
{
    None,
    // Buffs
    Heal, Shield, DamageBuff, Cleanse,
    // Debuffs
    Burn, Poison, Slow, Weaken, Root
}
```

---

## Luồng Logic Chính

### 1. Khởi tạo
`PlayerController.SetupUI()` → `SpellCardPanel.Initialize(selectedSpells, playerID)` → `SpellCardManager.InitializeHand(owner, spells)` → Instantiate `SpellCardBase` buttons.

### 2. Chọn Card + Xác nhận
- UI click → `SpellCardBase.OnCardClicked()` → `SpellCardManager.SelectCard(card, caster, sourceRect)`
- Validation: `card != null` + `HasEnoughMP()` + không đang confirming/targeting
- Nếu OK → `SpellCardConfirmUI.Show()`:
  - Card bay từ vị trí gốc lên giữa màn hình (fly animation)
  - Hiện overlay mờ nền
  - Hiện 2 nút: ✓ Confirm / ✗ Cancel
- **Confirm** → `OnConfirmCard()` → `_isTargeting = true` → vào chế độ chọn target
- **Cancel** → `OnCancelCard()` → `DeselectCard()` → quay về trạng thái bình thường

### 3. Sử dụng Card

| Bước | Hành động |
|------|-----------|
| 1 | Lấy unit từ tile (`MapManager.GetUnitAtTile`) |
| 2 | `ValidateTarget()` theo `SpellTargetType` |
| 3 | `MPManager.SpendMP()` |
| 4 | `_actionLefts.Value--` |
| 5 | `card.Cast(caster, target)` → `effect.Apply()` |
| 6 | Xóa card nếu `consumeOnUse` |
| 7 | `GameMediator.NotifySpellCardUsed()` |

### 4. Target Validation

| TargetType | Điều kiện |
|------------|-----------|
| SingleAlly | target cùng owner |
| SingleEnemy | target khác owner |
| Self | target cùng owner |
| AnyUnit | luôn hợp lệ (trừ dead) |
| AllAllies / AllEnemies | chưa implement |

---

## Effect System (Strategy Pattern)

```csharp
public interface ISpellEffect
{
    void Apply(PlayerID caster, UnitController target);
}
```

**Thêm Effect mới:** Tạo class `[Serializable]` implement `ISpellEffect` → tự động hiện trong Inspector dropdown → không cần sửa Factory/switch.

### Các Effect hiện có

| Effect | Loại | Fields | Cơ chế |
|--------|------|--------|--------|
| **HealEffect** | Tức thời | `healAmount` | `target.Heal()` |
| **ShieldEffect** | Buff có duration | `shieldValue`, `duration` | `ModifyIncomingDamage()` trừ shield |
| **DamageBuffEffect** | Buff có duration | `damageBonus`, `duration` | `GetDamageBonus()` cộng damage |
| **CleanseEffect** | Tức thời | — | `BuffDebuffHandler.ClearAll()` |
| **RootEffect** | Debuff có duration | `duration` | `IsRooted()` chặn `CanMove()` |
| **DamageEffect** | Tức thời | `damage` | `target.TakeDamage()` |

---

## Buff/Debuff System

### ActiveStatusEffect (struct)

```csharp
public struct ActiveStatusEffect
{
    public StatusEffectType Type;
    public int Value;              // Shield amount, damage bonus, etc.
    public int RemainingTurns;
    public PlayerID SourcePlayer;
    public bool TickTurn();        // Giảm 1 turn, return true nếu hết hạn
}
```

### BuffDebuffHandler — Các method chính

| Method | Chức năng |
|--------|-----------|
| `AddEffect(effect)` | Thêm effect (cùng loại → thay thế) |
| `TickEffects()` | Đầu turn: giảm duration, apply tick damage (Burn/Poison), xóa hết hạn |
| `GetShieldValue()` / `GetDamageBonus()` | Lấy bonus từ active effects |
| `IsRooted()` | Kiểm tra bị Root |
| `ModifyIncomingDamage(raw)` | `max(0, raw - shield)` |
| `ClearAll()` | Xóa toàn bộ effects |

**Quy tắc:** Cùng loại → thay thế. Khác loại → stack. Burn = damage cố định/lượt. Poison = damage scaling/lượt.

**Events:** `OnEffectAdded` / `OnEffectRemoved` → `BuffIconDisplay` cập nhật icon.

### Tích hợp UnitController
- `TakeDamage()` → `_buffHandler.ModifyIncomingDamage(damage)` (Shield)
- `CanMove()` → `_buffHandler.IsRooted()` (Root)
- `OnTurnBegin()` → `_buffHandler.TickEffects()` (tick + giảm duration)

---

## Event System

| Source | Event | Subscribers |
|--------|-------|-------------|
| SpellCardManager | `OnCardSelected(SpellCardData)` | SpellCardPanel (highlight) |
| SpellCardManager | `OnCardDeselected` | SpellCardPanel (reset) |
| GameMediator | `OnSpellCardUsed(SpellCardData, PlayerID)` | SpellCardPanel, UI |
| GameMediator | `OnHandChanged(PlayerID)` | SpellCardPanel (rebuild) |
| BuffDebuffHandler | `OnEffectAdded/Removed(ActiveStatusEffect)` | BuffIconDisplay |

---

## Design Patterns (tóm tắt)

| Pattern | Áp dụng | Mục đích |
|---------|---------|----------|
| **Strategy** | `ISpellEffect` + `[SerializeReference]` | Mỗi effect implement interface, gán polymorphic trên SO |
| **Observer** | Events trong SpellCardManager, BuffDebuffHandler, GameMediator | Decouple UI/logic |
| **Singleton** | `SpellCardManager.Instance` | Global access |
| **Data-Driven** | `SpellCardData` (ScriptableObject) | Tạo spell mới trong Editor, không sửa code |

---

## Trạng thái & TODO

| Hạng mục | Status |
|----------|--------|
| SpellCardData, SpellCardManager, SpellCardPanel UI | ✅ |
| Effects: Heal, Shield, DamageBuff, Damage, Cleanse, Root | ✅ |
| BuffDebuffHandler + BuffIconDisplay | ✅ |
| Burn/Poison tick effects | ✅ |
| AllAllies / AllEnemies targeting | ⚠️ Chưa implement validate |
| Slow/Weaken debuffs | ⚠️ Enum có, logic chưa tích hợp |
