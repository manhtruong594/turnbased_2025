# Particle được chọn cho Skills

Nguồn đối chiếu: `Assets/Docs/Systems docs/SKILLS_STATUS_EFFECTS_UNIT_CLASSES_REFERENCE.md`, phần `Skills`.
Tên thư mục theo dạng Class_Skill; skill dùng chung nối các class bằng dấu +. Mỗi thư mục con chứa bản sao prefab cho một skill. Prefab gốc giữ nguyên trong `Assets/EffectsPack`; bản sao vẫn tham chiếu đến các material/texture gốc. Các prefab này chưa được gán vào skill runtime.

| Skill | Bản sao | Vai trò | Prefab nguồn |
|---|---|---|---|
| Normal Arrow | `Archer_Normal Arrow/VFX_Arrow_Shot.prefab` | Mũi tên | `Assets/EffectsPack/Vefects/Anime Stylized VFX/Shared/Particles/VFX_Arrow_Shot.prefab` |
| Normal Arrow | `Archer_Normal Arrow/VFX_Arrow_Shot_Impact.prefab` | Trúng đích | `Assets/EffectsPack/Vefects/Anime Stylized VFX/Shared/Particles/VFX_Arrow_Shot_Impact.prefab` |
| Multi Strike Arrow | `Archer_Multi Strike Arrow/VFX_Arrow_Shot.prefab` | Lặp lại cho từng mũi tên | `Assets/EffectsPack/Vefects/Anime Stylized VFX/Shared/Particles/VFX_Arrow_Shot.prefab` |
| Multi Strike Arrow | `Archer_Multi Strike Arrow/VFX_Arrow_Shot_Impact.prefab` | Trúng đích | `Assets/EffectsPack/Vefects/Anime Stylized VFX/Shared/Particles/VFX_Arrow_Shot_Impact.prefab` |
| Multi Strike Arrow | `Archer_Multi Strike Arrow/Hit_frost.prefab` | Chỉ dùng khi gây Freeze | `Assets/EffectsPack/Lana Studio/Casual RPG VFX/Prefabs/Range_attack/Hit_frost.prefab` |
| Assassin Bleed | `Assassin_Assassin Bleed/VFX_Piercing_Dark.prefab` | Đòn đâm | `Assets/EffectsPack/Vefects/Stylized VFX/Stylized VFX Shuriken/Skills/Slashes Piercing/Dark/VFX_Piercing_Dark.prefab` |
| Assassin Bleed | `Assassin_Assassin Bleed/VFX_Blood_Burst_01.prefab` | Trúng đích và gây Bleed | `Assets/EffectsPack/Vefects/Stylized AoE VFX/VFX/Blood/Particles/VFX_Blood_Burst_01.prefab` |
| Shadow Dual Strike | `Assassin_Shadow Dual Strike/VFX_Dash.prefab` | Dịch chuyển đến mục tiêu | `Assets/EffectsPack/Vefects/Anime Stylized VFX/Shared/Particles/VFX_Dash.prefab` |
| Shadow Dual Strike | `Assassin_Shadow Dual Strike/VFX_Slash_Dark.prefab` | Phát hai lần chém | `Assets/EffectsPack/Vefects/Stylized VFX/Stylized VFX Shuriken/Skills/Slashes Piercing/Dark/VFX_Slash_Dark.prefab` |
| Shadow Dual Strike | `Assassin_Shadow Dual Strike/VFX_Buff_Cast.prefab` | Chỉ dùng khi nhận ShadowStep | `Assets/EffectsPack/Vefects/Anime Stylized VFX/Shared/Particles/VFX_Buff_Cast.prefab` |
| Mace Smash | `Berserker_Mace Smash/Hit_stone.prefab` | Va chạm búa | `Assets/EffectsPack/Lana Studio/Casual RPG VFX/Prefabs/Slash/Hit_stone.prefab` |
| Blood Hammer | `Berserker_Blood Hammer/VFX_Blood_Burst_01.prefab` | Va chạm và sát thương lan | `Assets/EffectsPack/Vefects/Stylized AoE VFX/VFX/Blood/Particles/VFX_Blood_Burst_01.prefab` |
| Blood Hammer | `Berserker_Blood Hammer/VFX_Buff_Cast.prefab` | Chỉ dùng khi nhận BloodRage | `Assets/EffectsPack/Vefects/Anime Stylized VFX/Shared/Particles/VFX_Buff_Cast.prefab` |
| Normal Attack | `Axe Soldier+Knight_Normal Attack/VFX_Slash_Generic.prefab` | Đòn đánh Axe Soldier hoặc Knight | `Assets/EffectsPack/Vefects/Stylized VFX/Stylized VFX Shuriken/Skills/Slashes Piercing/Generic/VFX_Slash_Generic.prefab` |
| Crescent Slash | `Axe Soldier_Crescent Slash/VFX_Slash_Generic.prefab` | Chém theo đường thẳng | `Assets/EffectsPack/Vefects/Stylized VFX/Stylized VFX Shuriken/Skills/Slashes Piercing/Generic/VFX_Slash_Generic.prefab` |
| Crescent Slash | `Axe Soldier_Crescent Slash/Shield_gold.prefab` | Chỉ dùng khi tạo Shield | `Assets/EffectsPack/Lana Studio/Casual RPG VFX/Prefabs/Shields/Shield_gold.prefab` |
| Holy Sword Stance | `Knight_Holy Sword Stance/VFX_Slash_Generic_Add.prefab` | Chém theo đường thẳng | `Assets/EffectsPack/Vefects/Stylized VFX/Stylized VFX Shuriken/Skills/Slashes Piercing/Generic/VFX_Slash_Generic_Add.prefab` |
| Holy Sword Stance | `Knight_Holy Sword Stance/Shield_gold.prefab` | StanceGuard | `Assets/EffectsPack/Lana Studio/Casual RPG VFX/Prefabs/Shields/Shield_gold.prefab` |
| Fireball | `Magician_Fireball/VFX_Fireball.prefab` | Quả cầu lửa | `Assets/EffectsPack/Vefects/Anime Stylized VFX/Shared/Particles/VFX_Fireball.prefab` |
| Fireball | `Magician_Fireball/VFX_Fire_Burst_01.prefab` | Nổ tại ô mục tiêu | `Assets/EffectsPack/Vefects/Stylized AoE VFX/VFX/Fire/Particles/VFX_Fire_Burst_01.prefab` |
| Fire Seal | `Magician_Fire Seal/VFX_Fire_Burst_01.prefab` | Sát thương diện rộng | `Assets/EffectsPack/Vefects/Stylized AoE VFX/VFX/Fire/Particles/VFX_Fire_Burst_01.prefab` |
| Fire Seal | `Magician_Fire Seal/VFX_Fire_Area_01.prefab` | Burning Ground | `Assets/EffectsPack/Vefects/Stylized AoE VFX/VFX/Fire/Particles/VFX_Fire_Area_01.prefab` |
| Heavy Smash Attack | `Smasher_Heavy Smash Attack/Hit_stone.prefab` | Va chạm búa | `Assets/EffectsPack/Lana Studio/Casual RPG VFX/Prefabs/Slash/Hit_stone.prefab` |
| Earthquake | `Smasher_Earthquake/VFX_Earth_Burst_01.prefab` | Chấn động khi đánh trúng | `Assets/EffectsPack/Vefects/Stylized AoE VFX/VFX/Earth/Particles/VFX_Earth_Burst_01.prefab` |
| Earthquake | `Smasher_Earthquake/Stun.prefab` | Chỉ dùng khi va chạm vật cản gây Stun | `Assets/EffectsPack/Lana Studio/Casual RPG VFX/Prefabs/States/Stun.prefab` |
| Heal Skill | `Archer+Assassin+Axe Soldier+Berserker+Knight+Magician+Smasher_Heal Skill/VFX_Heal_Cast.prefab` | Hồi Máu bản thân | `Assets/EffectsPack/Vefects/Anime Stylized VFX/Shared/Particles/VFX_Heal_Cast.prefab` |
| Quick Slash | `Militia_Quick Slash/VFX_Slash_Generic.prefab` | Chém nhanh | `Assets/EffectsPack/Vefects/Stylized VFX/Stylized VFX Shuriken/Skills/Slashes Piercing/Generic/VFX_Slash_Generic.prefab` |
| Power Strike | `Militia_Power Strike/VFX_Slash_Generic_Add.prefab` | Đòn chém mạnh | `Assets/EffectsPack/Vefects/Stylized VFX/Stylized VFX Shuriken/Skills/Slashes Piercing/Generic/VFX_Slash_Generic_Add.prefab` |
| Bandage | `Militia_Bandage/Regeneration_health.prefab` | Hồi Máu bản thân | `Assets/EffectsPack/Lana Studio/Casual RPG VFX/Prefabs/Regeneration/Regeneration_health.prefab` |
| Staff Tap | `Herbalist_Staff Tap/VFX_Basic_Attack.prefab` | Đòn đánh đơn | `Assets/EffectsPack/Vefects/Anime Stylized VFX/Shared/Particles/VFX_Basic_Attack.prefab` |
| Field Remedy | `Herbalist_Field Remedy/VFX_Heal_Cast.prefab` | Hồi Máu cho mục tiêu | `Assets/EffectsPack/Vefects/Anime Stylized VFX/Shared/Particles/VFX_Heal_Cast.prefab` |
| Purification | `Herbalist_Purification/VFX_Heal_Burst_01.prefab` | Xóa debuff | `Assets/EffectsPack/Vefects/Stylized AoE VFX/VFX/Heal/Particles/VFX_Heal_Burst_01.prefab` |
| Purification | `Herbalist_Purification/Shield_gold.prefab` | Chỉ dùng khi xóa được debuff | `Assets/EffectsPack/Lana Studio/Casual RPG VFX/Prefabs/Shields/Shield_gold.prefab` |
| Verdict Stroke | `Adjudicator_Verdict Stroke/VFX_Piercing_Generic.prefab` | Đòn đánh tầm 2 ô | `Assets/EffectsPack/Vefects/Stylized VFX/Stylized VFX Shuriken/Skills/Slashes Piercing/Generic/VFX_Piercing_Generic.prefab` |
| Accusation | `Adjudicator_Accusation/VFX_Piercing_Dark.prefab` | Đòn đánh đơn | `Assets/EffectsPack/Vefects/Stylized VFX/Stylized VFX Shuriken/Skills/Slashes Piercing/Dark/VFX_Piercing_Dark.prefab` |
| Accusation | `Adjudicator_Accusation/VFX_Debuff_Cast.prefab` | Chỉ dùng khi gây GuardBreak | `Assets/EffectsPack/Vefects/Anime Stylized VFX/Shared/Particles/VFX_Debuff_Cast.prefab` |
| Forbidden Seal | `Adjudicator_Forbidden Seal/VFX_Dark_Burst_01.prefab` | Đòn đánh diện rộng | `Assets/EffectsPack/Vefects/Stylized AoE VFX/VFX/Dark/Particles/VFX_Dark_Burst_01.prefab` |
| Forbidden Seal | `Adjudicator_Forbidden Seal/VFX_Debuff_Cast.prefab` | Weaken và HealBan | `Assets/EffectsPack/Vefects/Anime Stylized VFX/Shared/Particles/VFX_Debuff_Cast.prefab` |
| Flame Thrower | `None_Flame Thrower/Flamethrower.prefab` | Luồng lửa gây sát thương theo chu kỳ | `Assets/EffectsPack/Lana Studio/Casual RPG VFX/Prefabs/Fire/Flamethrower.prefab` |
