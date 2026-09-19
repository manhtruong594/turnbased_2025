# Tra cứu Skills, StatusEffectType và class unit

## 1. Phạm vi và nguồn dữ liệu

Tài liệu này phản ánh code và asset hiện có tại thời điểm cập nhật. Số liệu gameplay lấy trực tiếp từ
`UnitData` và skill asset, không lấy từ checklist thiết kế.

Nguồn chính:

- Skill contract và pipeline: `Assets/Scripts/Skills/ISkill.cs`, `Assets/Scripts/Skills/SkillBase.cs`.
- Skill implementation: `Assets/Scripts/Skills/*Skill.cs`.
- Skill cấu hình: `Assets/Scripts/Data/SkillData/**/*.asset`.
- Status: `Assets/Scripts/SpellCard/SpellCardData.cs`,
  `Assets/Scripts/SpellCard/BuffDebuffHandler.cs`.
- Unit roster: các `UnitData` asset trực tiếp trong `Assets/Scripts/Data`.

Trong project không có `UnitClass` enum và cũng không có subclass `ArcherUnit`, `KnightUnit`, v.v.
Mỗi class unit là một `UnitData` asset dùng chung runtime `UnitController`, `UnitAttack`,
`UnitAnimator`, `SkillEffectRunner` và `BuffDebuffHandler`.

Ba class chính thức Dân Binh, Dược Sư và Phán Quan nằm ở mục 9 của
[Characters Overview](Characters_Overview.md). Bốn implementation riêng và chín skill asset đã có.
Ba class chưa có `UnitData`/prefab nên chưa thuộc roster có thể spawn; bảng roster vẫn tách riêng
bảy unit đã tích hợp và ba unit đang chờ tích hợp.

## 2. Quy ước chung của Skill System

### 2.1. Contract

Mọi skill runtime implement `ISkill`. Các skill hiện tại đều kế thừa `SkillBase`, là
`ScriptableObject` chứa:

- Metadata: `skillName`, `description`, `skillType`, `icon`.
- Chi phí và giới hạn: `cooldown`, `mpCost`, `range`, `targetTypes`.
- VFX: cast, release, projectile, impact, thời điểm áp effect và spawn point.
- Runtime: `currentCooldown`, execution context và cờ `_isExecuting`.

Khi unit khởi tạo, `UnitAttack` clone từng asset trong `UnitData.StartingSkills`. Cooldown vì vậy nằm
trên bản clone runtime, không ghi ngược vào asset dùng chung.

### 2.2. `SkillType`

| Giá trị | Tên | Ý nghĩa trong code |
|---:|---|---|
| 0 | `Normal` | Không dùng MP và không bắt đầu cooldown, bất kể `mpCost`/`cooldown` trên asset. |
| 1 | `Active` | Skill chủ động thông thường. |
| 2 | `Passive` | Được khai báo nhưng pipeline hiện tại không có cơ chế trigger passive riêng. |
| 3 | `Ultimate` | Dùng MP và cooldown như skill không phải `Normal`; chưa có tài nguyên ultimate riêng. |
| 4 | `BuffAndDebuff` | Dùng MP và cooldown như skill không phải `Normal`. |

### 2.3. `TargetType`

`TargetType` là flags mask: `None = 0`, `Ally = 1`, `Enemy = 2`, `Self = 4`,
`EmptyTile = 8`. Ví dụ `10` là `Enemy | EmptyTile`, `13` là
`Ally | Self | EmptyTile`.

Validation mặc định của `SkillBase.CanUse` theo thứ tự: cooldown → MP → range → target mask → điều
kiện riêng. `FireballSkill` và `MagicianFireSealSkill` có validation riêng.

`Execute` gửi command qua `LocalMatchAuthority`. Khi command được chấp nhận,
`ExecuteAuthorized` trừ MP, resolve `ExecuteEffect`, bắt đầu cooldown và gọi
`FinishTurnActionsAuthorized`. Animation/VFX chỉ được phát qua `PublishAfterCommit`; `ApplyEffect`
được giữ cho caller presentation cũ và không resolve gameplay lần hai.

## 3. Tất cả skill implementation

Project có 16 class skill cụ thể.

| Class | Mô tả hành vi hiện tại | Status liên quan | Asset hiện có |
|---|---|---|---|
| `NormalAttackSkill` | Gây `CurrentDamage × multipleDmg` lên một unit tại ô đích. | Không | 7 asset |
| `HealSkill` | Hồi `CurrentDamage × multiple`; chỉ dùng khi mục tiêu chưa đầy HP. `HealBan` có thể chặn hồi máu tại `UnitController.Heal`. | Đọc `HealBan` gián tiếp | 3 asset |
| `FireballSkill` | Gây sát thương diện rộng lên mọi enemy trong `aoeRadius`; có thể chọn ô trống. | Không | `Fireball.asset` |
| `MultiArrowSkill` | Bắn nhiều hit. Hit đầu dùng multiplier cố định; hit sau dùng multiplier ngẫu nhiên và có cơ hội áp một `ISpellEffect`. | Phụ thuộc `CurrentArrowEffect`; asset hiện tại dùng `FreezeEffect` | `Multi strike arrow.asset` |
| `FlameThrowerSkill` | Gây damage theo chu kỳ lên một enemy tại ô đích trong một khoảng thời gian. | Không | Chưa có asset trong `SkillData` |
| `AssassinBleedSkill` | Gây direct damage rồi áp `Bleed`. | Tạo `Bleed` | `AssassinBleedSkill.asset` |
| `AssassinShadowDualStrikeSkill` | Tìm ô trống cạnh target, teleport caster, đánh hai hit; hit hai mạnh hơn nếu target còn không quá ngưỡng HP. Hạ target sẽ nhận `ShadowStep`. | Tạo `ShadowStep` | `Assassin Shadow Dual Strike.asset` |
| `BerserkerBloodHammerSkill` | Damage lớn mục tiêu chính, damage các enemy lân cận, tự mất phần trăm HP hiện tại nhưng không chết. Hạ mục tiêu chính sẽ nhận `BloodRage`. | Tạo `BloodRage` | `Berserker Blood Hammer.asset` |
| `KnightHolySwordStanceSkill` | Gây damage theo đường thẳng; enemy đầu tiên nhận `GuardBreak`; caster nhận `StanceGuard`, kéo dài hơn nếu đứng trên capture point. | Tạo `GuardBreak`, `StanceGuard` | `Knight Holy Sword Stance.asset` |
| `MagicianFireSealSkill` | Damage diện rộng, tăng damage lên target đang `Burn`, có xác suất gây `Burn` và tạo `BurningGround` trên các ô ảnh hưởng. | Đọc/tạo `Burn` | `Magician Fire Seal.asset` |
| `SmasherEarthquakeSkill` | Gây damage và đẩy target. Nếu bị chặn thì gây collision damage và `Stun`; target miễn displacement không nhận collision effect. | Tạo `Stun`, đọc `StanceGuard` qua displacement utility | `Smasher Earthquake.asset` |
| `HalberdierCrescentSlashSkill` | Đánh enemy theo đường thẳng. Target đầu nhận damage chính; các target sau nhận damage phụ. Có thể gây `HealBan` và luôn tạo `Shield` cho caster. | Tạo `HealBan`, `Shield` | `Halberdier Crescent Slash.asset` |
| `MilitiaPowerStrikeSkill` | Gây 175% damage; nếu target có `Shield` trước hit thì nhân thêm 1,3 trước khi qua pipeline nhận damage. | Đọc `Shield` | `Militia Power Strike.asset` |
| `HerbalistCleanseSkill` | Chỉ nhận target có debuff; xóa toàn bộ debuff và cấp `Shield = 30% CurrentDamage` trong 2 lượt nếu số debuff thực sự giảm. | Xóa debuff, tạo `Shield` | `Herbalist Purification.asset` |
| `AdjudicatorAccusationSkill` | Gây 100% damage; target còn sống nhận `GuardBreak(20)` trong 2 lượt. | Tạo `GuardBreak` | `Adjudicator Accusation.asset` |
| `AdjudicatorForbiddenSealSkill` | Gây 160% damage lên enemy trong radius 1; enemy còn sống nhận `Weaken(25)` và có 50% nhận `HealBan`, đều trong 2 lượt. | Tạo `Weaken`, `HealBan` | `Adjudicator Forbidden Seal.asset` |

## 4. Skill asset đang cấu hình

`CD` là cooldown; `MP` là `mpCost`; `Range` dùng khoảng cách từ `MapManager.GetDistance` trừ khi
implementation ghi khác.

| Asset | Class | `skillName` | Type | MP / CD / Range | Target | Cấu hình effect chính | Unit dùng |
|---|---|---|---|---|---|---|---|
| `Archer_skill/Normal arrow.asset` | `NormalAttackSkill` | `NormalAttack` | `Normal` | 0 / 0 / 1 | Enemy + EmptyTile | 125% damage | Archer |
| `Archer_skill/Multi strike arrow.asset` | `MultiArrowSkill` | `Multi strike arrow` | `Ultimate` | 1 / 0 / 5 | Enemy + EmptyTile | 4 mũi: 120% ở hit đầu, 40–70% ở hit sau; mỗi hit sau có 25% áp `Freeze` 2 lượt | Archer |
| `Assassin_skill/AssassinBleedSkill.asset` | `AssassinBleedSkill` | `Unnamed Skill` | `Normal` | 0 / 0 / 1 | Enemy + EmptyTile | 100% damage; `Bleed` 10 damage/lượt trong 3 lượt | Assassin |
| `Assassin_skill/Assassin Shadow Dual Strike.asset` | `AssassinShadowDualStrikeSkill` | `Anh Bo Song Sat` | `Ultimate` | 1 / 4 / 3 | Enemy + EmptyTile | 90% + 90%; hit hai thành 140% khi target còn ≤ 40% HP; `ShadowStep` 1 lượt khi kết liễu | Assassin |
| `berserker_skill/mace Smash.asset` | `NormalAttackSkill` | `NormalAttack` | `Normal` | 0 / 0 / 1 | Enemy | 125% damage | Berserker |
| `berserker_skill/Berserker Blood Hammer.asset` | `BerserkerBloodHammerSkill` | `Huyet Chuy Pha Tran` | `Ultimate` | 1 / 4 / 1 | Enemy | 220% target chính; 110% enemy lân cận; mất 10% HP hiện tại; `BloodRage` +20% damage trong 1 lượt khi kết liễu | Berserker |
| `Slash attack.asset` | `NormalAttackSkill` | `NormalAttack` | `Normal` | 0 / 0 / 1 | Enemy + EmptyTile | 125% damage | Axe Soldier, Knight |
| `halberdier skill/Halberdier Crescent Slash.asset` | `HalberdierCrescentSlashSkill` | `Nguyet Nha Tram Tran` | `Ultimate` | 1 / 4 / 2 | Enemy | Line 2 ô; 170% target đầu, 100% target sau; `HealBan` 2 lượt theo điều kiện; `Shield = 100% CurrentDamage` trong 2 lượt | Axe Soldier |
| `knight skill/Knight Holy Sword Stance.asset` | `KnightHolySwordStanceSkill` | `Thanh Kiem Lap The` | `Ultimate` | 1 / 4 / 1 | Enemy | Line 3 ô, 160%; `GuardBreak` +20% incoming damage trong 1 lượt; `StanceGuard` giảm 20% incoming damage trong 1 lượt, cộng 1 lượt trên capture point | Knight |
| `magician skill/Fireball.asset` | `FireballSkill` | `FireBall` | `Ultimate` | 1 / 3 / 2 | Validation riêng | 125% damage trong radius 2 quanh ô đích | Magician |
| `magician skill/Magician Fire Seal.asset` | `MagicianFireSealSkill` | `Hoa An Thien Tru` | `Ultimate` | 1 / 5 / 3 | Enemy hoặc EmptyTile | Radius 1, 140%; +25% multiplier nếu target đang `Burn`; 25% gây `Burn`; ground có 50% gây `Burn`; burn = 40% damage, 2 lượt; ground tồn tại 2 lượt | Magician |
| `smasher skill/Heavy slash attack.asset` | `NormalAttackSkill` | `NormalAttack` | `Normal` | 0 / 0 / 5 | Enemy | 125% damage | Smasher |
| `smasher skill/Smasher Earthquake.asset` | `SmasherEarthquakeSkill` | `Dia Chan Toai Son` | `Ultimate` | 1 / 4 / 1 | Enemy + EmptyTile | 180% damage; đẩy 1 ô; nếu bị chặn: thêm 50% damage và `Stun` 1 lượt | Smasher |
| `Heal skill.asset` | `HealSkill` | `Unnamed Skill` | `BuffAndDebuff` | 1 / 2 / 2 | Ally + Self + EmptyTile | Hồi 150% `CurrentDamage` | Cả 7 class unit |
| `militia skill/Militia Quick Slash.asset` | `NormalAttackSkill` | `Chém Nhanh` | `Normal` | 0 / 0 / 1 | Enemy | 100% damage | Dân Binh — chờ `UnitData` |
| `militia skill/Militia Power Strike.asset` | `MilitiaPowerStrikeSkill` | `Dồn Sức` | `Active` | 1 / 2 / 1 | Enemy | 175% damage; nhân thêm 1,3 nếu target có `Shield` | Dân Binh — chờ `UnitData` |
| `militia skill/Militia Bandage.asset` | `HealSkill` | `Băng Bó` | `BuffAndDebuff` | 1 / 3 / 0 | Self | Hồi 150% `CurrentDamage` | Dân Binh — chờ `UnitData` |
| `herbalist skill/Herbalist Staff Tap.asset` | `NormalAttackSkill` | `Gõ Gậy` | `Normal` | 0 / 0 / 1 | Enemy | 80% damage | Dược Sư — chờ `UnitData` |
| `herbalist skill/Herbalist Field Remedy.asset` | `HealSkill` | `Đắp Thuốc` | `BuffAndDebuff` | 1 / 2 / 2 | Ally + Self | Hồi 200% `CurrentDamage` | Dược Sư — chờ `UnitData` |
| `herbalist skill/Herbalist Purification.asset` | `HerbalistCleanseSkill` | `Giải Uế` | `BuffAndDebuff` | 1 / 3 / 2 | Ally + Self | Xóa debuff; nếu xóa thành công, tạo `Shield = 30% CurrentDamage` trong 2 lượt | Dược Sư — chờ `UnitData` |
| `adjudicator skill/Adjudicator Verdict Stroke.asset` | `NormalAttackSkill` | `Bút Phán` | `Normal` | 0 / 0 / 2 | Enemy | 100% damage | Phán Quan — chờ `UnitData` |
| `adjudicator skill/Adjudicator Accusation.asset` | `AdjudicatorAccusationSkill` | `Cáo Trạng` | `Active` | 1 / 3 / 3 | Enemy | 100% damage; `GuardBreak(20)` trong 2 lượt nếu target còn sống | Phán Quan — chờ `UnitData` |
| `adjudicator skill/Adjudicator Forbidden Seal.asset` | `AdjudicatorForbiddenSealSkill` | `Đại Ấn Cấm Sinh` | `Ultimate` | 3 / 5 / 3 | Enemy + EmptyTile | Radius 1, 160% damage; `Weaken(25)` và 50% `HealBan`, 2 lượt | Phán Quan — chờ `UnitData` |

Đường dẫn gốc của các asset trong bảng: `Assets/Scripts/Data/SkillData`.

## 5. `StatusEffectType`

### 5.1. Vòng đời chung

`BuffDebuffHandler` lưu `ActiveStatusEffect` gồm `Type`, `Value`, `InitialDuration`, `RemainingTurns` và
`SourcePlayer`. Thêm lại cùng type sẽ xóa instance cũ rồi thêm instance mới, tức refresh/thay thế chứ
không stack cùng type. Ở `UnitController.OnTurnBegin`, hazard được kiểm tra trước, cooldown giảm, sau đó
mọi status áp tick effect rồi giảm `RemainingTurns`. Status vẫn có hiệu lực trong lượt cuối và được xóa
khi unit hoàn tất action; nhờ vậy effect có duration 1 vẫn tác động đủ một lượt.

`IsDebuff()` chỉ trả `true` cho dải debuff được liệt kê rõ. Mọi giá trị khác ngoài `None` được
`IsBuff()` coi là buff.

### 5.2. Bảng tra cứu

| Giá trị | Type | Nhóm | Hành vi runtime hiện tại | Nguồn tạo hiện có | Mức triển khai |
|---:|---|---|---|---|---|
| 0 | `None` | Không có | Sentinel; không spawn VFX. | Không | Hoàn chỉnh |
| 1 | `HealOverTime` | Buff | Hồi `Value` HP ở đầu mỗi lượt; vẫn bị `HealBan` chặn. | `HealOverTimeEffect` | Có hành vi; chưa có spell asset |
| 2 | `Shield` | Buff | Trừ `Value` khỏi mỗi lần nhận damage, sau khi áp modifier phần trăm. Không phải pool giáp bị tiêu hao. | `ShieldEffect`, `HalberdierCrescentSlashSkill`, `HerbalistCleanseSkill` | Có hành vi |
| 3 | `DamageBuff` | Buff | Cộng phẳng `Value` vào base damage trước bonus phần trăm. | `DamageBuffEffect`, `GameplayTestTool` | Có hành vi |
| 4 | `CleanseOverTime` | Buff | Xóa mọi debuff ở đầu lượt trước khi damage-over-time tick. Không xóa buff. | `CleanseOverTimeEffect` | Có hành vi; chưa có spell asset |
| 5 | `BloodRage` | Buff | Cộng `Value`% outgoing damage và cộng 1 move range. | `BerserkerBloodHammerSkill` | Có hành vi |
| 6 | `StanceGuard` | Buff | Giảm incoming damage theo `Value`% và miễn displacement. | `KnightHolySwordStanceSkill` | Có hành vi |
| 7 | `ShadowStep` | Buff | Không thể bị direct-target qua validation chung/`UnitAttack`; vẫn có thể bị effect diện rộng không direct-target. | `AssassinShadowDualStrikeSkill` | Có hành vi |
| 100 | `Burn` | Debuff | Nhận `Value` damage ở đầu lượt. Có thể được tạo khi trúng/đứng trên `BurningGround`. | `MagicianFireSealSkill`, `TileHazardManager`, test tool | Có hành vi |
| 101 | `Poison` | Debuff | Damage đầu lượt tăng tuyến tính: `Value × số tick đã chịu` (1×, 2×, 3×...). | `GameplayTestTool` | Có hành vi |
| 102 | `Slow` | Debuff | Giảm move range theo `Value`%, chặn ở mức 100%. `Freeze` bị phá sớm tạo `Slow(20)` trong số lượt còn lại. | `SlowEffect`, cơ chế phá `Freeze` | Có hành vi; chưa có spell asset |
| 103 | `Weaken` | Debuff | Giảm outgoing damage theo `Value`%; được tính cùng bonus phần trăm của `BloodRage`. | `WeakenEffect`, `AdjudicatorForbiddenSealSkill` | Có hành vi |
| 104 | `Root` | Debuff | `UnitController.CanMove()` trả `false`; không trực tiếp chặn attack/action. | `RootEffect`, `Root Spell.asset`, test tool | Có hành vi giới hạn di chuyển |
| 105 | `Stun` | Debuff | Chặn chọn unit, di chuyển, attack và skill trong lượt; cleanse có thể gỡ stun trước khi lượt kết thúc. | `StunEffect`, `Stun Spell.asset`, `SmasherEarthquakeSkill`, test tool | Có hành vi |
| 106 | `Freeze` | Debuff | Chặn toàn bộ hành động trong 2 lượt. Hit damage trực tiếp đầu tiên nhận thêm 20% damage và phá `Freeze`; số lượt còn lại chuyển thành `Slow(20)`. Damage-over-time không phá `Freeze`. | `FreezeEffect`; `Multi strike arrow.asset` | Có hành vi |
| 107 | `Bleed` | Debuff | Nhận `Value` damage ở đầu lượt. | `BleedEffect`, `AssassinBleedSkill` | Có hành vi |
| 108 | `GuardBreak` | Debuff | Tăng incoming damage theo `Value`%. | `KnightHolySwordStanceSkill`, `AdjudicatorAccusationSkill` | Có hành vi |
| 109 | `HealBan` | Debuff | `UnitController.Heal()` từ chối mọi hồi máu. | `HalberdierCrescentSlashSkill`, `AdjudicatorForbiddenSealSkill` | Có hành vi |

### 5.3. Spell effect có thể tạo status

Các implementation `ISpellEffect` nằm trong `Assets/Scripts/SpellCard/SpellEffects.cs`:

- Tạo status: `HealOverTimeEffect`, `ShieldEffect`, `DamageBuffEffect`, `CleanseOverTimeEffect`,
  `SlowEffect`, `WeakenEffect`, `RootEffect`, `StunEffect`, `FreezeEffect`, `BleedEffect`.
- Không tạo status: `HealEffect`, `DamageEffect`, `CleanseEffect`, `CompositeEffect`.
- Spell asset hiện có: `Damage Spell`, `Root Spell`, `Shield Spell`, `Stun Spell` trong
  `Assets/Scripts/Data/SpellData`.

`BuffIconData.asset` hiện có entry cho `Shield`, `StanceGuard`, `Burn`, `Poison`, `Weaken`, `Root`,
`Stun`, `Freeze`, `Bleed`, `GuardBreak`, `HealBan`. Các type còn lại dùng fallback icon/color của
`BuffIconData` nếu UI yêu cầu hiển thị.

## 6. Class unit và trạng thái roster

### 6.1. Roster đã tích hợp

Thông số chung của cả 7 `UnitData` hiện tại: `Health = 100`, `BaseDamage = 10`, `spawnCost = 3`.
YAML cũ còn `moveSpeed = 5`, nhưng `UnitData`/`UnitRuntimeStats` hiện không khai báo hoặc đọc field
này nên nó không tham gia gameplay. Danh sách skill giữ đúng thứ tự serialized; `UnitAttack` chọn
skill đầu tiên làm `_normalSkill`/skill mặc định dù asset đó có thể không mang type `Normal`.

| UnitData asset | Tên unit | Move range | Starting skills theo thứ tự | Vai trò suy ra từ implementation |
|---|---|---:|---|---|
| `Archer Data.asset` | Archer | 6 | `Normal arrow` → `Multi strike arrow` → `Heal skill` | Ranged damage, multi-hit và hồi phục |
| `Assassin data.asset` | Assassin | 6 | `AssassinBleedSkill` → `Assassin Shadow Dual Strike` → `Heal skill` | Bleed, áp sát, execute và né direct-target |
| `Axe data.asset` | Axe Soldier | 6 | `Slash attack` → `Halberdier Crescent Slash` → `Heal skill` | Melee line/reach; đây là data asset gần nhất với class Halberdier, không có `Halberdier Data.asset` riêng |
| `Berserker Data.asset` | Berserker | 5 | `mace Smash` → `Berserker Blood Hammer` → `Heal skill` | Bruiser, self-cost và damage tăng khi kết liễu |
| `Knight Data.asset` | Knight | 10 | `Slash attack` → `Knight Holy Sword Stance` → `Heal skill` | Line damage, guard và giữ capture point |
| `Magician data.asset` | Magician | 5 | `Fireball` → `Magician Fire Seal` → `Heal skill` | AoE, burn và tile hazard |
| `Smasher data.asset` | Smasher | 6 | `Heavy slash attack` → `Smasher Earthquake` → `Heal skill` | Damage tầm xa theo asset hiện tại, knockback và collision stun |

### 6.2. Class chính thức đang chờ tích hợp unit

Ba class dưới đây đã có implementation skill và skill asset, nhưng chưa có `UnitData`/prefab nên
chưa thể chọn trong deck hoặc spawn. Các chỉ số là cấu hình chính thức cần dùng khi tạo `UnitData`.

| Class | HP | Base damage | Spawn cost | Move range | `StartingSkills` bắt buộc theo thứ tự |
|---|---:|---:|---:|---:|---|
| Dân Binh — Militia | 70 | 8 | 2 | 5 | `Militia Quick Slash` → `Militia Power Strike` → `Militia Bandage` |
| Dược Sư — Herbalist | 55 | 8 | 2 | 5 | `Herbalist Staff Tap` → `Herbalist Field Remedy` → `Herbalist Purification` |
| Phán Quan — Adjudicator | 140 | 14 | 6 | 4 | `Adjudicator Verdict Stroke` → `Adjudicator Accusation` → `Adjudicator Forbidden Seal` |

## 7. Runtime class dùng chung cho unit

| Class | Trách nhiệm |
|---|---|
| `UnitData` | Data tĩnh: tên, mô tả, HP, damage, spawn cost, movement, icon, starting skills. |
| `UnitController` | State runtime, di chuyển/teleport, damage/heal, turn action, status tick và death. |
| `UnitRuntimeStats` | Bản sao chỉ số runtime và trạng thái action/owner/map. |
| `UnitAttack` | Clone skill, tạo button, chọn skill, attack mode, range/target execution và cooldown tick. |
| `UnitAnimator` | Phát animation movement, attack/skill, hit và death. |
| `SkillEffectRunner` | Điều phối VFX/projectile và gọi thời điểm áp effect theo animation cue. |
| `BuffDebuffHandler` | Lưu, refresh, tick và truy vấn modifier/status của unit. |
| `HealthBar` | Hiển thị HP. |
| `UnitSpawner` / `SpawnPoint` | Tạo unit theo owner và vị trí spawn. |
