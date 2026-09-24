# Spell and Skill Icon Prompts

These prompts cover the 9 spell and 23 skill data assets in `Assets/Scripts/Data`. Attach the image provided in the conversation as the style reference when generating each image. Existing game icons were not used as references.

Combine the shared prompt with one individual prompt below for each icon.

## Shared prompt

> Use the provided image as the style reference. Create a square 1:1 fantasy RPG icon in its simple painterly style: one large, glowing symbol floating at the center of a nearly black, softly textured background. Use the same restrained light, soft vignette, warm gold accents, and generous dark space. The symbol must read clearly at 64×64 pixels. Add only a faint curved magical trail if needed. Use one dominant color specified below. Keep the composition and detail level consistent across the set. Do not copy the healing symbol or hand into unrelated icons. No extra objects, scenery, text, numbers, logo, watermark, border, or UI frame.

## Spells

1. **Damage Spell:** A glowing jagged magic bolt. Dominant color: crimson.
2. **Root Spell:** A single thick root twisted into a binding knot. Dominant color: forest green.
3. **Shield Spell:** A simple glowing shield. Dominant color: sapphire blue.
4. **Stun Spell:** A sharp lightning bolt with a bright center. Dominant color: electric yellow.
5. **Dawn Absolution:** A clean dawn sunburst. Dominant color: warm gold.
6. **Merciful Prison:** A heart-shaped cage made of vines. Dominant color: emerald green.
7. **Ashen Ultimatum:** A cracked hourglass holding glowing ash. Dominant color: ember orange.
8. **Ember Sacrifice:** A heart shaped from a single flame. Dominant color: blood red.
9. **Stonewall Rise:** A solid stone pillar rising upward. Dominant color: earthy amber.

## Skills

1. **NormalAttack (Slash attack):** A steel sword with one bright diagonal slash. Dominant color: steel silver.
2. **Heal Skill:** A softly glowing healing heart. Dominant color: jade green.
3. **Normal Arrow:** A single arrow pointing forward. Dominant color: woodland green.
4. **Multi strike arrow:** Three arrows grouped in one clean silhouette. Dominant color: icy cyan.
5. **Assassin Bleed:** A slim dagger with a red glowing edge. Dominant color: deep crimson.
6. **Assassin Shadow Dual Strike:** Two crossed daggers emerging from shadow. Dominant color: dark violet.
7. **Mace Smash:** A heavy spiked mace head. Dominant color: bronze.
8. **Berserker Blood Hammer:** A broad warhammer head with a red glow. Dominant color: blood red.
9. **Halberdier Crescent Slash:** A crescent halberd blade with one curved slash. Dominant color: turquoise.
10. **Knight Holy Sword Stance:** An upright holy sword. Dominant color: radiant gold.
11. **Fire Ball:** A single fireball with a bright center. Dominant color: fiery orange.
12. **Magician Fire Seal:** A plain circular fire seal with a small flame at its center. Dominant color: scarlet orange.
13. **Herbalist Staff Tap:** The glowing tip of a wooden staff. Dominant color: warm olive green.
14. **Herbalist Field Remedy:** A single glowing medicinal leaf. Dominant color: fresh green.
15. **Herbalist Purification:** A clear glowing water droplet. Dominant color: teal.
16. **Militia Quick Slash:** A short sword with one narrow slash. Dominant color: steel blue.
17. **Militia Power Strike:** A broad sword with a bright, forceful edge. Dominant color: amber gold.
18. **Militia Bandage:** A simple glowing bandage roll. Dominant color: soft green.
19. **Adjudicator Verdict Stroke:** A judge's brush with one glowing stroke, without writing. Dominant color: ivory gold.
20. **Adjudicator Accusation:** A pointed judgment seal without letters or symbols. Dominant color: royal purple.
21. **Adjudicator Forbidden Seal:** A closed arcane seal without writing. Dominant color: deep violet.
22. **Heavy smash attack:** A large blunt weapon head with a bright impact edge. Dominant color: iron gray.
23. **Smasher Earthquake:** A cracked stone pillar. Dominant color: ochre brown.

## Status Effects — prompt icon vector

Danh sách gồm toàn bộ 17 giá trị hiệu ứng trong `StatusEffectType`: 6 buff và 11 debuff. Không tạo icon cho `None`. `BurningGround` là hiệu ứng trên ô bản đồ (`TileHazardType`), không phải status trên unit. Icon giữ nguyên màu có sẵn trong ảnh; UI không tô màu lại sprite. Không dùng icon hiện có trong game làm mẫu.

Ghép prompt chung với **một** prompt riêng bên dưới cho mỗi icon.

### Prompt chung cho Status Effects

> Tạo một icon trạng thái game fantasy dạng **vector SVG thuần**, khung vuông `viewBox="0 0 64 64"`, nền trong suốt. Dùng một biểu tượng chính ở giữa, silhouette rõ khi thu nhỏ còn 24×24 px. Thiết kế bằng các mảng màu phẳng khép kín, đường viền đồng nhất, góc và khoảng trống đủ rộng; tối đa hai sắc độ của một màu chủ đạo, màu xanh cho buff và màu đỏ cho debuff. Giữ cùng độ dày nét, tỉ lệ, khoảng đệm và mức đơn giản cho cả bộ. Không dùng phong cách tranh vẽ của icon spell/skill, không gradient, glow, texture, đổ bóng, hiệu ứng 3D, chi tiết li ti, cảnh nền, nhân vật, chữ, số, logo, watermark hoặc khung UI. Chỉ thay hình biểu tượng theo mô tả riêng; xuất SVG với hình/path vector chỉnh sửa được, không nhúng ảnh raster.

### Buff

1. **HealOverTime — Hồi máu theo lượt:** Một trái tim cân đối với đường cong tuần hoàn ôm một bên, gợi dòng hồi phục lặp lại; trái tim vẫn là hình chính.
2. **Shield — Khiên bảo vệ:** Một tấm khiên nguyên vẹn, bản rộng, có lõi sáng đơn giản ở giữa; đường bao chắc và dễ nhận ra.
3. **DamageBuff — Tăng sát thương:** Một lưỡi kiếm thẳng hướng lên, thân kiếm nở rộng gần mũi để gợi sức mạnh tăng thêm; silhouette mạnh, gọn.
4. **BloodRage — Cuồng huyết:** Một nắm đấm siết chặt với mép ngoài nhọn như lửa bùng, thể hiện cơn cuồng nộ; giữ một khối chính liền mạch.
5. **StanceGuard — Thế thủ:** Một tấm khiên dựng đứng và cắm vững xuống mặt đất bằng chân khiên nhọn, khác rõ với khiên bảo vệ thông thường.
6. **ShadowStep — Bước bóng:** Một dấu chân đang tan dần thành hai mảng bóng kéo về phía sau, gợi né tránh và khó bị nhắm trực tiếp.

### Debuff

1. **Burn — Thiêu đốt:** Một ngọn lửa dựng đứng với lõi lửa rỗng đơn giản, đầu nhọn và chân rộng; không thêm củi hoặc cảnh cháy.
2. **Poison — Trúng độc:** Một giọt độc đầu nhọn với một bọt khí lớn bên trong, hình giọt bất đối xứng để phân biệt với hồi máu.
3. **Slow — Chậm:** Một chiếc ủng hướng về trước nhưng bị kéo giật lại bởi một vệt cong dày, thể hiện di chuyển chậm mà vẫn có thể bước.
4. **Weaken — Suy yếu:** Một thanh kiếm cụp xuống, lưỡi có một vết nứt lớn gần chuôi; phân biệt rõ với kiếm dựng lên của `DamageBuff`.
5. **Root — Trói chân:** Một chiếc ủng đứng yên bị một dây rễ to quấn chặt quanh cổ chân; dây rễ và ủng tạo thành một silhouette thống nhất.
6. **Stun — Choáng:** Một tia sét gấp khúc nằm trong vòng nổ ngắn, góc cạnh; diễn tả cú sốc làm mất khả năng hành động.
7. **Freeze — Đóng băng:** Một tinh thể băng sáu nhánh lớn, đối xứng, có lõi hình thoi; tránh các nhánh mảnh dễ mất nét.
8. **Bleed — Chảy máu:** Một giọt máu lớn bị một vết cắt chéo tách nhẹ ở phần trên; giữ đường bao giọt máu rõ ràng.
9. **GuardBreak — Phá thủ:** Một tấm khiên bị chẻ bởi một vết nứt lớn chạy xuyên từ đỉnh xuống đáy, hai nửa hơi tách nhau; khác với `Shield` nguyên vẹn.
10. **HealBan — Cấm hồi máu:** Một trái tim đơn giản bị gạch chéo bằng một vạch dày, thể hiện hồi máu bị ngăn; không dùng chữ hoặc dấu cộng.
11. **AshenUltimatum — Dấu ấn tối hậu tro tàn:** Một dấu ấn tròn bằng tro nứt vỡ, có lõi than hình thoi và một khe hở rõ trên vòng ngoài; gợi lời nguyền đang chờ kích hoạt, không dùng đồng hồ cát.
