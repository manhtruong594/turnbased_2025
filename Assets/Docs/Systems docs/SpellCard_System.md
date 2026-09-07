# Spell Card System

## 1. Mục tiêu

Spell card cho phép người chơi dùng MP để tạo hiệu ứng ngoài action riêng của unit. Hệ thống gồm data,
hand, targeting, confirmation, effect strategy, buff/debuff, UI và VFX.

## 2. Thành phần

| Thành phần | Trách nhiệm |
|---|---|
| `SpellCardData` | Metadata, MP cost, consume rule, target type, range, effect và VFX |
| `ISpellEffect` | Contract áp effect lên target |
| `SpellCardManager` | Hand, state, validation, range, selection, use và event |
| `SpellCardState` | Idle, Targeting và Confirming |
| `SpellCardPanel` | Tạo/refresh card UI cho một phe |
| `SpellCardItem` | Hiển thị card và chuyển click vào manager |
| `SpellCardConfirmUI` | Confirm/cancel trước khi cast |
| `BuffDebuffHandler` | Lưu status, tick duration và modifier runtime |
| `BuffIconDisplay` | Đồng bộ status với icon trên unit |
| `EffectManager` | Spawn visual theo `StatusEffectType` |

## 3. Data

`SpellCardData` có:

- `spellName`, `description`, `icon`.
- `mpCost`, `consumeOnUse`.
- `targetType`, `range`.
- `[SerializeReference] ISpellEffect spellEffect`.
- `castVfxPrefab`, `impactVfxPrefab`.

Effect dùng managed reference và custom property drawer. Khi đổi tên/move class effect phải có kế hoạch
migration; nếu type không resolve, dữ liệu effect trong asset có thể mất.

## 4. Target type

| Giá trị | Ý nghĩa | Trạng thái |
|---|---|---|
| `SingleAlly` | Một unit cùng phe | Hiện có |
| `SingleEnemy` | Một unit đối địch | Hiện có |
| `Self` | Caster/player context | Hiện có, cần kiểm chứng setup |
| `AnyUnit` | Một unit bất kỳ | Hiện có |
| `AllAllies` | Toàn bộ unit cùng phe | Chưa hoàn chỉnh |
| `AllEnemies` | Toàn bộ unit đối địch | Chưa hoàn chỉnh |

Area target phải quy định rõ range áp theo target trung tâm hay toàn map và card bị consume một lần cho
toàn bộ tập mục tiêu.

## 5. Effect hiện có

| Effect | Kết quả |
|---|---|
| `HealEffect` | Hồi HP nếu target nhận heal được |
| `HealOverTimeEffect` | Hồi HP ở đầu mỗi lượt theo value/duration |
| `ShieldEffect` | Thêm shield status theo value/duration |
| `DamageBuffEffect` | Tăng damage theo duration |
| `CleanseEffect` | Xóa toàn bộ debuff ngay lập tức, giữ nguyên buff |
| `CleanseOverTimeEffect` | Xóa toàn bộ debuff ở đầu mỗi lượt trong duration |
| `SlowEffect` | Giảm move range theo phần trăm |
| `WeakenEffect` | Giảm outgoing damage theo phần trăm |
| `RootEffect` | Cấm move |
| `DamageEffect` | Gây damage trực tiếp |
| `StunEffect` | Bỏ toàn bộ action của lượt bị stun |
| `FreezeEffect` | Khóa hành động 2 lượt; hit trực tiếp đầu tiên phá Freeze và nhận thêm 20% damage |
| `BleedEffect` | Damage theo turn |
| `CompositeEffect` | Chạy danh sách effect theo thứ tự |

Data assets hiện có: Damage Spell, Root Spell, Shield Spell và Stun Spell trong
`Assets/Scripts/Data/SpellData`.

## 6. Status

`BuffDebuffHandler` hỗ trợ add, tick, remove, query buff/debuff và các modifier như shield, damage,
move, heal ban, untargetable, displacement immunity và rooted. `StatusEffectType` có thêm các giá trị
phục vụ skill như `BloodRage`, `StanceGuard`, `ShadowStep`, `GuardBreak`, `HealBan`.

`Slow` giảm move range theo `Value`%, `Weaken` giảm outgoing damage theo `Value`%. `Freeze` khóa toàn
bộ hành động trong 2 lượt. Hit damage trực tiếp đầu tiên nhận thêm 20% damage, phá `Freeze` và chuyển
số lượt còn lại thành `Slow(20)`; damage-over-time không phá `Freeze`. Status tick ở đầu lượt nhưng
chỉ bị xóa khi unit hoàn tất action để duration 1 vẫn có hiệu lực trong lượt cuối.

## 7. Luồng sử dụng card

1. `PlayerController` khởi tạo `SpellCardPanel` từ `PlayerDataSO.SelectedSpells`.
2. Panel gọi `SpellCardManager.InitializeHand` cho phe.
3. Click card gọi `SelectCard`; manager kiểm tra turn và MP.
4. State chuyển sang Targeting và highlight vùng hợp lệ.
5. Chọn target hợp lệ chuyển sang Confirming.
6. Confirm kiểm tra lại target/MP trước transaction.
7. Card cast effect, spawn VFX, trừ MP và bị remove nếu `consumeOnUse`.
8. Manager phát spell-used/hand-changed và trở về Idle.

Cancel hoặc validation fail phải trở về state an toàn mà không trừ MP/consume card.

## 8. Quy tắc transaction

- Validation được chạy lại tại thời điểm confirm, không chỉ lúc chọn target.
- MP chỉ trừ khi cast được commit.
- `consumeOnUse` chỉ remove đúng một card sau commit.
- Effect composite áp theo thứ tự; target chết giữa danh sách phải được từng effect xử lý an toàn.
- UI hand, MP và state cập nhật từ nguồn runtime, không tự đoán kết quả.
- Cast/impact VFX không quyết định gameplay success.

## 9. Known gaps

| Mức | Vấn đề |
|---|---|
| P0 | Chưa có regression cho cancel/invalid/dead target và transaction MP/card |
| P1 | `AllAllies`/`AllEnemies` validation và execution chưa hoàn chỉnh |
| P1 | AI chưa dùng spell card |
| P2 | Tooltip/combat log/fallback icon cần hoàn thiện |

## 10. Kiểm chứng

1. Đúng/sai turn, đủ/thiếu MP.
2. Mọi `SpellTargetType`, gồm target chết và target đổi trạng thái trước confirm.
3. Cancel ở Targeting và Confirming.
4. Consume true/false; hand và MP chỉ đổi một lần.
5. Status add, refresh/stack, tick và remove đúng luật.
6. Composite effect khi effect trước giết target.
7. Scene restart không giữ selection, subscriber hoặc pooled VFX cũ.
