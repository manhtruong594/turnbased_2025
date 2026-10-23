# Workflow Giai Đoạn 0: Thiết Kế & Tạo Model Synty Sidekick (UCP tích hợp)

Tài liệu quy định quy trình thiết kế hình thể, bộ phận trang phục, bảng màu và xuất Model Prefab từ **Synty Sidekick** thông qua **UCP (Unity Control Protocol)** dựa trên tài liệu thiết kế:
- [Characters_Overview.md](file:///e:/UnityProject/My%20Turnbased/turnbased_2025/Assets/Docs/Overview/Characters_Overview.md)
- [SKILLS_STATUS_EFFECTS_UNIT_CLASSES_REFERENCE.md](file:///e:/UnityProject/My%20Turnbased/turnbased_2025/Assets/Docs/Overview/SKILLS_STATUS_EFFECTS_UNIT_CLASSES_REFERENCE.md)

Để xem quy trình tổng thể (Animator, Vũ khí, Animation Events, UnitData và Catalog), xem [Master Workflow: Character_Animation_Weapon_Setup_Workflow.md](file:///e:/UnityProject/My%20Turnbased/turnbased_2025/Assets/Docs/Workflow/Character_Animation_Weapon_Setup_Workflow.md).

---

## 1. Bảng Định Hướng Visual, Vũ Khí & Asset Cho 10 Classes

Căn cứ vào vai trò chiến thuật, bộ kỹ năng và asset thực tế trong dự án:

| Class | Vai trò & Đặc điểm kỹ năng | Body Sliders (Type / Size / Muscle) | Bộ phận tiêu biểu (Parts) | Tông màu chủ đạo | Prefab Vũ Khí (`Assets/MyGame/Resource/Unit/Weapons/`) | Socket Khung Xương |
|---|---|---|---|---|---|---|
| **Berserker** | Bruiser cận chiến, `Blood Hammer`, đốt máu `BloodRage` | Type: -90<br/>Size: +30<br/>Muscle: +80 | Giáp da thô, ngực trần, găng sắt | Đỏ thẫm, nâu da, đen kim loại | `Weapon_Berserker.prefab` | `prop_r` (fallback: `hand_r` -> `ring_01_r`) |
| **Knight** | Tanker, `Holy Sword Stance`, `StanceGuard` | Type: -60<br/>Size: +20<br/>Muscle: +40 | Giáp tấm toàn thân, mũ giáp kín, áo choàng ngắn | Bạc kim loại, vàng ánh kim, xanh hoàng gia | `Weapon_Knight.prefab` | `prop_r` (fallback: `hand_r` -> `ring_01_r`) |
| **Magician** | Ranged AoE, `Fireball`, `Fire Seal` | Type: 0<br/>Size: -30<br/>Muscle: -20 | Áo choàng pháp sư, mũ chóp hoặc trùm đầu | Đỏ cam lửa, vàng rực, đen than | `Weapon_Magician.prefab` | `prop_r` (fallback: `hand_r` -> `ring_01_r`) |
| **Smasher** | Phá đội hình, đẩy lùi (`Earthquake`), gây choáng | Type: -100<br/>Size: +70<br/>Muscle: +100 | Thân hình hộ pháp, giáp vai nặng, đai lưng to | Xám đá, nâu đất, đồng thau | `Weapon_Smasher.prefab` | `prop_r` (fallback: `hand_r` -> `ring_01_r`) |
| **Halberdier** | Melee tầm xa 2 ô, lướt `Crescent Slash`, cấm hồi máu | Type: -40<br/>Size: +10<br/>Muscle: +30 | Giáp xích kết hợp giáp tấm nhẹ, mũ lính canh | Thép xanh, trắng bạc, vải viền xanh | `Weapon_Halberdier.prefab` (hoặc `Spear_Medium.prefab`) | `prop_r` (fallback: `hand_r` -> `ring_01_r`) |
| **Assassin** | Cơ động, áp sát nhanh, `Shadow Dual Strike`, gây `Bleed` | Type: -20<br/>Size: -30<br/>Muscle: +20 | Quần áo da bó sát, khăn che mặt, mũ trùm | Đen bóng đêm, tím thẫm, xám tro | `Weapon_Assassin.prefab` | `prop_r` (fallback: `hand_r` -> `ring_01_r`) |
| **Archer** | Bắn tỉa tầm xa, `Multi Strike Arrow`, gây `Freeze` | Type: 0<br/>Size: -15<br/>Muscle: +10 | Áo da nhẹ, bao tay bắn cung, ống tên lưng | Xanh lá rừng, nâu da thuộc, xanh băng | `Weapon_Archer.prefab` | `prop_l` / `hand_l` (cung cầm tay trái) |
| **Militia** | Bộ binh giá rẻ, `Power Strike` phá giáp, tự `Bandage` | Type: -30<br/>Size: 0<br/>Muscle: +10 | Quần áo vải thô, giáp ngực đơn sơ, mũ nồi sắt | Nâu đất mộc, xám vải lanh, da thô | `Weapon_Militia.prefab` | `prop_r` (fallback: `hand_r` -> `ring_01_r`) |
| **Herbalist** | Hồi máu (`Field Remedy`), giải debuff, khiên (`Purification`) | Type: +20<br/>Size: -20<br/>Muscle: -30 | Áo vải choàng nhẹ, túi da đeo hông chứa thảo dược | Xanh lá thảo mộc, nâu gỗ, trắng ngà | `Weapon_Herbalist.prefab` | `prop_r` (`pos: (-0.014, -0.15, -0.033)`, `rot: (180, 0, 0)`) |
| **Adjudicator** | Phán quan, debuff diện rộng, `Verdict Stroke` | Type: -10<br/>Size: +15<br/>Muscle: +15 | Áo choàng tư tế uy nghi, cổ áo đứng, vương miện | Đen huyền bí, viền vàng kim, đỏ ruby | `Weapon_Adjudicator.prefab` | `prop_r` (`pos: (-0.014, -0.15, -0.033)`, `rot: (180, 0, 0)`) |

---

## 2. Tham Số Cấu Hình Synty Sidekick Trên `character-builder`

Script [`CharacterBuilder.cs`](file:///e:/UnityProject/My%20Turnbased/turnbased_2025/Assets/Scripts/Editor/CharacterBuilder.cs) hỗ trợ 2 chế độ lấy Model Prefab:
1. **`generateSidekickModel = true`**: Truy vấn trực tiếp SQLite DB của Synty Sidekick, ghép các `partNames`, áp dụng blendshapes (`bodyTypeBlend`, `bodySizeBlend`, `musclesBlend`), bake bảng màu và tạo Humanoid Avatar tự động.
2. **`generateSidekickModel = false`**: Sử dụng Model Prefab đã xuất sẵn từ cửa sổ Sidekick Character Creator tại đường dẫn `modelPrefabPath` (mặc định `Assets/MyGame/Prefabs/Model/<className>_M/<className>.prefab`).

| Tham số | Kiểu dữ liệu | Mặc định | Mô tả |
|---|---|---|---|
| `generateSidekickModel` | `bool` | `false` | `true`: tự động query Sidekick DB, combine mesh, bake texture và tạo Humanoid Avatar |
| `bodyTypeBlend` | `float` | `-10` | Hình thể (-100: vạm vỡ/nam tính, +100: mảnh mai/nữ tính) |
| `bodySizeBlend` | `float` | `15` | Độ to béo / gầy |
| `musclesBlend` | `float` | `15` | Độ cơ bắp |
| `partNames` | `string[]` | *Bộ Adjudicator* | Danh sách tên mesh part (`SidekickPart`) ghép vào nhân vật |
| `palette` | `ColorPaletteParams`| `null` | Nhóm màu: `skinColor`, `hairColor`, `eyeColor`, `primaryColor`, `secondaryColor`, `trimColor`, `metalColor`, `jewelColor` |
| `primaryColor`, ... | `string` | *(Mặc định)* | Các trường mã hex màu flat tương đương (thuận tiện khi truyền qua CLI) |
| `colorOverrides` | `ColorOverride[]` | `null` | Mảng ghi đè màu trực tiếp theo từ khóa tên thuộc tính |
| `modelPrefabPath` | `string` | `Assets/MyGame/Prefabs/Model/<className>_M/<className>.prefab` | Đường dẫn lưu hoặc đọc Model Prefab |

---

## 3. Thực Thi Tạo Model & Dựng Unit Bằng Lệnh UCP

> [!NOTE]
> Mọi lệnh `action: "build"` trong [`CharacterBuilder.cs`](file:///e:/UnityProject/My%20Turnbased/turnbased_2025/Assets/Scripts/Editor/CharacterBuilder.cs) sẽ tự động liên kết `UnitAnimator._effectSpawnPoint` vào Transform của vũ khí vừa gắn và gọi `MatchContentCatalogBuilder.Rebuild()` ở cuối quy trình. Đối với các class chưa có sẵn AnimatorController riêng (như `Berserker`, `Halberdier`), người dùng cần tự tạo Controller/OverrideController trước khi truyền vào `animatorControllerPath`.

### Kịch bản A: Tự động sinh Model Sidekick + Lắp ráp Unit Prefab (Ví dụ: Berserker)
```powershell
ucp --bridge-update-policy off exec run character-builder --parameters '{
  "action": "build",
  "className": "Berserker",
  "generateSidekickModel": true,
  "bodyTypeBlend": -90,
  "bodySizeBlend": 30,
  "musclesBlend": 80,
  "primaryColor": "8B0000",
  "secondaryColor": "4A2E18",
  "trimColor": "1F1F1F",
  "metalColor": "4B4B4B",
  "partNames": [
    "SK_HUMN_BASE_02_01HEAD_HU01",
    "SK_HUMN_BASE_03_03EBRL_HU01",
    "SK_HUMN_BASE_03_04EBRR_HU01",
    "SK_HUMN_BASE_01_05EYEL_HU01",
    "SK_HUMN_BASE_01_06EYER_HU01",
    "SK_HUMN_BASE_01_07EARL_HU01",
    "SK_HUMN_BASE_01_08EARR_HU01",
    "SK_HUMN_BASE_02_35NOSE_HU01",
    "SK_HUMN_BASE_01_36TETH_HU01",
    "SK_HUMN_BASE_01_37TONG_HU01",
    "SK_FANT_WARR_01_10TORS_HU01",
    "SK_FANT_WARR_01_17HIPS_HU01",
    "SK_FANT_WARR_01_18LEGL_HU01",
    "SK_FANT_WARR_01_19LEGR_HU01",
    "SK_FANT_WARR_01_20FOTL_HU01",
    "SK_FANT_WARR_01_21FOTR_HU01"
  ],
  "weaponPrefabPath": "Assets/MyGame/Resource/Unit/Weapons/Weapon_Berserker.prefab",
  "weaponBoneName": "prop_r",
  "unitDataPath": "Assets/Scripts/Data/Berserker Data.asset",
  "skillPaths": [
    "Assets/Scripts/Data/SkillData/Berserker/HeavyAxeSlash.asset",
    "Assets/Scripts/Data/SkillData/Berserker/BloodHammer.asset"
  ],
  "animatorControllerPath": "Assets/MyGame/Animations/Axe.controller"
}' --json
```

### Kịch bản B: Dùng Model Prefab có sẵn từ Sidekick Editor (Ví dụ: Herbalist)
```powershell
ucp --bridge-update-policy off exec run character-builder --parameters '{
  "action": "build",
  "className": "Herbalist",
  "generateSidekickModel": false,
  "modelPrefabPath": "Assets/MyGame/Prefabs/Model/Herbalist_M/Herbalist.prefab",
  "weaponPrefabPath": "Assets/MyGame/Resource/Unit/Weapons/Weapon_Herbalist.prefab",
  "weaponBoneName": "prop_r",
  "unitDataPath": "Assets/Scripts/Data/Herbalist Data.asset",
  "skillPaths": [
    "Assets/Scripts/Data/SkillData/Herbalist/FieldRemedy.asset",
    "Assets/Scripts/Data/SkillData/Herbalist/Purification.asset",
    "Assets/Scripts/Data/SkillData/Herbalist/HerbalBlessing.asset"
  ],
  "animatorControllerPath": "Assets/MyGame/Animations/Herbalist_Override.overrideController"
}' --json
```

---

## 4. Bước Tiếp Theo Trong Chuỗi Workflow
Sau khi xuất Model Prefab và khởi tạo Unit Prefab, tiếp tục với (xác nhận lại trước khi tiếp tục)
- [Master Workflow: Character_Animation_Weapon_Setup_Workflow.md](file:///e:/UnityProject/My%20Turnbased/turnbased_2025/Assets/Docs/Workflow/Character_Animation_Weapon_Setup_Workflow.md)
- [Giai đoạn 1: Character_Animator_Avatar_Setup_Workflow.md](file:///e:/UnityProject/My%20Turnbased/turnbased_2025/Assets/Docs/Workflow/Character_Animator_Avatar_Setup_Workflow.md)
