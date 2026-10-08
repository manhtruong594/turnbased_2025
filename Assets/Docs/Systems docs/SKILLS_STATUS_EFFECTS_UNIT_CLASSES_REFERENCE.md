# Skills, Spell Cards và Status Effects

## Skills

| Skill | Class sử dụng | Mô tả |
|---|---|---|
| `Normal Arrow` | Archer | Bắn một mũi tên, gây sát thương bằng 125% sát thương hiện tại của người thi triển lên một kẻ địch. |
| `Multi Strike Arrow` | Archer | Bắn nhiều mũi tên liên tiếp. Mũi đầu gây 120% sát thương; các mũi sau gây sát thương giảm dần và có 25% xác suất gây `Freeze`. |
| `Assassin Bleed` | Assassin | Gây 100% sát thương lên một kẻ địch và gây `Bleed`, khiến mục tiêu nhận 10 sát thương vào đầu mỗi lượt trong 3 lượt. |
| `Shadow Dual Strike` | Assassin | Dịch chuyển đến cạnh mục tiêu và tấn công hai lần. Đòn thứ hai mạnh hơn khi mục tiêu còn ít Máu. Nếu hạ mục tiêu, người thi triển nhận `ShadowStep`. |
| `Mace Smash` | Berserker | Gây sát thương bằng 125% sát thương hiện tại của người thi triển lên một kẻ địch. |
| `Blood Hammer` | Berserker | Gây sát thương lớn lên mục tiêu chính và sát thương lan lên các kẻ địch lân cận, sau đó tiêu hao một phần Máu hiện tại nhưng không thể khiến người thi triển tử trận. Hạ mục tiêu chính sẽ nhận `BloodRage`. |
| `Normal Attack` | Axe Soldier, Knight | Gây sát thương bằng 125% sát thương hiện tại của người thi triển lên một kẻ địch. |
| `Crescent Slash` | Axe Soldier | Xoay rìu lướt theo đường thẳng dài 2 ô, gây sát thương cho mục tiêu trên đường lướt, áp dụng `HealBan` lên mục tiêu đầu tiên và tạo `Shield` cho người thi triển. Nếu mục tiêu đang ở trên capture point, chắc chắn áp dụng `HealBan`. |
| `Holy Sword Stance` | Knight | Chém các kẻ địch trên một đường thẳng dài 3 ô, gây `GuardBreak` lên kẻ địch đầu tiên trúng đòn, sau đó người thi triển nhận `StanceGuard`. |
| `Fireball` | Magician | Thi triển hỏa công, gây 125% sát thương lên tất cả kẻ địch trong phạm vi 2 ô quanh ô mục tiêu. |
| `Fire Seal` | Magician | Triệu hồi một biển lửa, gây sát thương lửa diện rộng, tăng sát thương lên mục tiêu đang bị `Burn`, có thể gây `Burn` và để lại `Burning Ground` trên mặt đất có khả năng gây `Burn`. |
| `Heavy Smash Attack` | Smasher | Bổ một nhát búa chí mạng, gây sát thương bằng 125% sát thương hiện tại của người thi triển lên một kẻ địch. |
| `Earthquake` | Smasher | Dồn xung lực vào cây búa giáng mạnh xuống đất, gây sát thương và đẩy lùi một kẻ địch. Nếu mục tiêu va chạm với địa hình, mục tiêu nhận thêm sát thương và bị `Stun`. |
| `Heal Skill` | Archer, Assassin, Axe Soldier, Berserker, Knight, Magician, Smasher | Hồi Máu cho bản thân bằng 150% sát thương hiện tại. |
| `Quick Slash` | Militia | Chém một đường kiếm nhanh khiến đối phương không kịp trở tay, gây 100% sát thương hiện tại lên một kẻ địch. |
| `Power Strike` | Militia | Dồn sức vào nhát chém quyết định, gây 175% sát thương hiện tại. Sát thương tăng thêm 30% nếu mục tiêu đang có `Shield`. |
| `Bandage` | Militia | Hồi Máu cho bản thân bằng 150% sát thương hiện tại. |
| `Staff Tap` | Herbalist | Vung gậy quật mạnh, gây 80% sát thương hiện tại lên một kẻ địch. |
| `Field Remedy` | Herbalist | Hồi Máu cho bản thân hoặc đồng minh bằng 200% sát thương hiện tại. |
| `Purification` | Herbalist | Xóa toàn bộ debuff trên mục tiêu. Nếu xóa thành công ít nhất một debuff, mục tiêu nhận `Shield` bằng 30% sát thương hiện tại trong 2 lượt. |
| `Verdict Stroke` | Adjudicator | Vung gậy phép tạo ra một luồng năng lượng bóng tối, gây 100% sát thương hiện tại lên một kẻ địch trong tầm 2 ô. |
| `Accusation` | Adjudicator | Bắn một chùm năng lượng bóng tối, gây 100% sát thương hiện tại. Nếu mục tiêu còn sống, mục tiêu nhận `GuardBreak` 20% trong 2 lượt. |
| `Forbidden Seal` | Adjudicator | Tạo một vùng cấm thuật, gây 160% sát thương trong bán kính 1 ô. Mỗi kẻ địch còn sống nhận `Weaken` 25% và có 50% xác suất nhận `HealBan` trong 2 lượt. |

## Spell Cards

| Spell Card | Mô tả |
|---|---|
| `Damage Spell` | Gây 4 sát thương lên một kẻ địch trên toàn bản đồ. |
| `Root Spell` | Gây `Root` lên một kẻ địch trong 2 lượt, khiến mục tiêu không thể di chuyển. |
| `Shield Spell` | Tạo `Shield` cho một đồng minh trong 2 lượt. |
| `Stun Spell` | Gây `Stun` lên một kẻ địch trong 1 lượt, khiến mục tiêu không thể hành động. |
| `Ashen Ultimatum` | Đánh dấu tối đa 2 kẻ địch. Cuối lượt kế tiếp của phe đó, mục tiêu chưa di chuyển nhận 20 sát thương, `Weaken` và `GuardBreak`. |
| `Dawn Absolution` | Xóa `Stun` và `Freeze` khỏi một đồng minh, sau đó hồi 20% Máu tối đa. |
| `Ember Sacrifice` | Hy sinh một đồng minh để nhận 3 MP và gây `Burn` lên các kẻ địch trong phạm vi 1 ô. |
| `Merciful Prison` | Hồi 25 Máu cho một unit, sau đó gây `Root` lên unit đó trong 2 lượt. |
| `Stonewall Rise` | Tạo núi đá không thể vượt qua trên ô trống chỉ định trong 4 lượt. |

## Status Effects

| Status Effect | Mô tả |
|---|---|
| `HealOverTime` | Hồi một lượng Máu cố định vào đầu mỗi lượt. Hiệu ứng hồi Máu bị `HealBan` ngăn chặn. |
| `Shield` | Giảm mỗi lần nhận sát thương đi một lượng cố định sau khi áp dụng các modifier phần trăm. Giá trị giảm sát thương không bị tiêu hao sau khi chặn đòn. |
| `DamageBuff` | Cộng một lượng cố định vào sát thương cơ bản. |
| `BloodRage` | Tăng sát thương gây ra theo phần trăm và tăng 1 tầm di chuyển. |
| `StanceGuard` | Giảm sát thương nhận vào theo phần trăm và miễn nhiễm hiệu ứng dịch chuyển. |
| `ShadowStep` | Khiến unit không thể bị chọn làm mục tiêu trực tiếp, nhưng vẫn có thể chịu hiệu ứng diện rộng. |
| `Burn` | Gây một lượng sát thương cố định vào đầu mỗi lượt. |
| `Poison` | Gây sát thương vào đầu mỗi lượt với lượng sát thương tăng tuyến tính theo số lần đã kích hoạt. |
| `Slow` | Giảm tầm di chuyển theo phần trăm. |
| `Weaken` | Giảm sát thương gây ra theo phần trăm. |
| `Root` | Ngăn unit di chuyển nhưng không trực tiếp ngăn tấn công hoặc dùng kỹ năng. |
| `Stun` | Ngăn unit được chọn, di chuyển, tấn công và dùng kỹ năng. |
| `Freeze` | Ngăn toàn bộ hành động. Đòn sát thương trực tiếp đầu tiên gây thêm 20% sát thương, phá `Freeze` và chuyển thời gian còn lại thành `Slow` 20%. |
| `Bleed` | Gây một lượng sát thương cố định vào đầu mỗi lượt. |
| `GuardBreak` | Tăng sát thương mục tiêu nhận vào theo phần trăm. |
| `HealBan` | Ngăn mọi hiệu ứng hồi Máu. |
| `AshenUltimatum` | Đánh dấu unit để kiểm tra ở cuối lượt của phe sở hữu. Nếu unit không di chuyển, unit nhận sát thương, `Weaken` và `GuardBreak`; sau đó dấu bị xóa. |

## VFX config của skill — 2026-10-07

Đã đối chiếu toàn bộ implementation trong `Assets/Scripts/Skills`, 23 asset trong `Assets/Scripts/Data/SkillData` và prefab trong `Assets/MyGame/Prefabs/Effects/Skill effects`. Chỉ thay đổi `vfxConfig` trong skill asset; giữ nguyên damage, MP, cooldown, range, target và dữ liệu status effect.

Các đường dẫn prefab dưới đây tương đối với `Assets/MyGame/Prefabs/Effects/Skill effects/Selected Skill Particles/`, có phần mở rộng `.prefab`. Ô `—` tương ứng reference rỗng.

| Skill | Cast | Release | Projectile | Impact | Vị trí Impact |
|---|---|---|---|---|---|
| `Normal Arrow` | — | `Archer_Normal Arrow/SkillArrowRelease` | `Archer_Normal Arrow/SkillArrowProjectile` | `Archer_Normal Arrow/VFX_Arrow_Shot_Impact` | `Target` |
| `Multi Strike Arrow` | — | Dùng chung với `Normal Arrow` | Dùng chung với `Normal Arrow` | Dùng chung với `Normal Arrow` | `Target` |
| `Assassin Bleed` | — | — | — | `Assassin_Assassin Bleed/VFX_Blood_Burst_01` | `Target` |
| `Shadow Dual Strike` | — | — | — | `Assassin_Shadow Dual Strike/VFX_Slash_Dark` | `Target` |
| `Mace Smash` | — | — | — | `Berserker_Mace Smash/Hit_stone` | `Target` |
| `Blood Hammer` | — | — | — | `Berserker_Blood Hammer/VFX_Blood_Burst_01` | `Target` |
| `Normal Attack` | — | — | — | `Axe Soldier+Knight_Normal Attack/VFX_Slash_Generic` | `EffectSpawnPoint` |
| `Crescent Slash` | — | — | — | `Axe Soldier_Crescent Slash/VFX_Slash_Generic` | `EffectSpawnPoint` |
| `Holy Sword Stance` | — | — | — | `Knight_Holy Sword Stance/VFX_Slash_Generic_Add` | `EffectSpawnPoint` |
| `Fireball` | — | — | — | `Magician_Fireball/VFX_Fire_Burst_01` | `Target` |
| `Fire Seal` | — | `Magician_Fire Seal/VFX_Fire_Area_01` | — | `Magician_Fire Seal/VFX_Fire_Burst_01` | `Target` |
| `Heavy Smash Attack` | — | — | — | `Smasher_Heavy Smash Attack/Hit_stone` | `Target` |
| `Earthquake` | — | — | — | `Smasher_Earthquake/VFX_Earth_Burst_01` | `Target` |
| `Heal Skill` | — | `Heal Skill/VFX_Heal_Cast` | — | `Heal Skill/VFX_Heal_Cast` | `Target` |
| `Quick Slash` | — | `Militia_Quick Slash/VFX_Slash_Generic` | — | `Militia_Quick Slash/VFX_Slash_Generic` | `EffectSpawnPoint` |
| `Power Strike` | — | `Militia_Power Strike/VFX_Slash_Generic_Add` | — | `Militia_Power Strike/VFX_Slash_Generic_Add` | `EffectSpawnPoint` |
| `Bandage` | — | `Militia_Bandage/Regeneration_health` | — | `Militia_Bandage/Regeneration_health` | `Target` |
| `Staff Tap` | — | — | — | `Herbalist_Staff Tap/VFX_Basic_Attack` | `Target` |
| `Field Remedy` | — | — | — | `Herbalist_Field Remedy/VFX_Heal_Cast` | `Target` |
| `Purification` | — | — | — | `Herbalist_Purification/VFX_Heal_Burst_01` | `Target` |
| `Verdict Stroke` | — | — | — | `Adjudicator_Verdict Stroke/VFX_Piercing_Generic` | `Target` |
| `Accusation` | `Adjudicator_Accusation/VFX_Debuff_Cast` | — | — | `Adjudicator_Accusation/VFX_Piercing_Dark` | `Target` |
| `Forbidden Seal` | `Adjudicator_Forbidden Seal/VFX_Debuff_Cast` | — | — | `Adjudicator_Forbidden Seal/VFX_Dark_Burst_01` | `Target` |

### Cue và lifecycle

- `Cast` đặt tại `Caster`. `Release` đặt tại `EffectSpawnPoint`, riêng `Heal Skill`, `Bandage`, `Fire Seal` đặt tại `Target`.
- Mũi tên dùng `OnProjectileImpact`; các skill khác dùng `OnAnimationImpact`. Đây là timing của presentation: gameplay vẫn resolve một lần qua `SkillBase.ExecuteAuthorized`; `ApplyEffect()` của animation là no-op.
- Hai prefab mới `SkillArrowRelease` và `SkillArrowProjectile` được Unity tạo từ `Firing` và `Projectile/Projectile 01` của `Archer_Normal Arrow/VFX_Arrow_Shot`. Projectile bỏ particle impact con, collision/sub-emitter và chuyển động particle độc lập; `Projectile` điều khiển đường bay, callback tạo impact, `PooledObject` trả pool. Hai skill Archer dùng chung prefab để tránh pool theo tên nhận hai bản khác nhau.
- 26 prefab one-shot được gắn `PooledObject`/`PooledVFX`, tắt loop và đặt `stopAction = None`. Prefab chưa có Particle System ở root được thêm root không emission, renderer tắt, để `PooledVFX` theo dõi particle con. Projectile dùng lifecycle riêng, không gắn `PooledVFX`.
- `SkillEffectRunner` đã bỏ lần spawn `Release` trùng khi không có projectile. `Projectile` clear particle khi launch và trả pool.
- `Heal Skill` có cả Release và Impact vì Archer phát Release, các class khác phát Impact. Militia hiện dùng `Archer.controller` trên prefab trong catalog; cấu hình cả hai cue để dùng được với controller hiện tại và các clip Impact của `Militia_Override`. Các clip liên quan hiện chỉ có một trong hai cue.
- Đã thêm `AnimEvent_Impact` cho ba clip `DualDaggers`, ba clip `Hammer`, `140_Orb_SkillDoom`, `Staff-Boost1` của Halberdier và clip `Staff-Cast-L-Buff1_loop`; đổi event `Hit` của `Staff-Attack1` sang callback `AnimEvent_Impact`. Timing khởi đầu là 45% clip; riêng Shadow Dual Strike là 35% và 65%. Các giá trị này chưa được đối chiếu hình ảnh động trong runtime.
- Không gán `Hit_frost`, `Stun`, `VFX_Buff_Cast`, `Shield_gold` vào cue luôn phát để tránh biểu diễn status có điều kiện như thể luôn xảy ra. Những status này cần presentation nhận kết quả thực tế của gameplay; bảng trên chỉ cấu hình VFX thi triển/đánh trúng.
- `FlameThrowerSkill` có implementation nhưng không có asset trong `SkillData` hoặc asset sử dụng class này được tìm thấy trong `Assets`; không tự tạo skill asset hoặc gán `None_Flame Thrower/Flamethrower` đang loop.

### Kiểm chứng và giới hạn

- Đã verify qua Unity Editor: 23 skill asset khớp mapping, 27 prefab được sử dụng có component/reference lifecycle cần thiết, không missing script, 26 one-shot không loop hoặc dùng stopAction phá pool; projectile có reference `pooledObject` và `trailEffect`.
- Đã kiểm tra 30 binding unit–skill của 10 unit prefab trong `MatchContentCatalog`: tất cả clip tương ứng có callback `AnimEvent_*`. Herbalist chưa có unit prefab trong catalog hiện tại; ba skill asset của Herbalist đã được cấu hình nhưng chưa xác nhận animation của unit này.
- Đã kiểm tra diff: toàn bộ dữ liệu serialized ngoài `vfxConfig` của 23 skill asset giữ nguyên. Unity reserialize prefab/importer theo version hiện tại; GUID asset hiện hữu giữ nguyên. Catalog có cơ chế tự cập nhật hash khi asset thay đổi.
- Unity compile thành công, không lỗi Console; còn warning `CS0414` tại `GameplayTestTool.cs(39,17)` ngoài phạm vi VFX. Reimport năm clip `068_DualDaggers_atkb`, `202_DualDaggers_DeafeningRoarShot`, `036_Hammer_PaladinAttackA`, `064_Hammer_PPJusticeAttackStart`, `093_Hammer_PSHolyFlame` khi giữ nguyên `hasTranslationDoF = 0` cũng phát animation import warning; chưa xác nhận retargeting trong runtime. Scene `SkillTest` giữ Edit Mode và không dirty.
- Chưa chạy Play Mode: hình ảnh, shader/material URP, scale/hướng, timing, valid/invalid target, target chết giữa effect và ít nhất ba vòng spawn/despawn chưa được xác minh. Một số prefab nguồn tham chiếu shader có hậu tố `_BIRP`; không coi reference hợp lệ là bằng chứng render đúng URP.
- `Fireball` hiện dùng clip chỉ có Impact nên cấu hình burst tại target, không dùng prefab particle demo `VFX_Fireball` như projectile. `Fire Seal` hiện phát Release nên phát vùng lửa one-shot; VFX không tồn tại theo bốn lượt `Burning Ground`. `Multi Strike Arrow` hiện có bốn hit gameplay nhưng clip `Ultimate Attack` chỉ có một Release, nên presentation hiện chỉ launch một projectile. Đồng bộ số mũi tên, đường chém diện rộng và VFX tồn tại theo status/hazard cần thay đổi presentation ngoài việc gán config này.
