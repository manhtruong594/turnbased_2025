# Kế hoạch Phase 4 — Content Expansion

## Mục tiêu

Mở rộng vertical slice bằng skill riêng cho roster, spell card, buff/debuff, AI local và độ hoàn thiện
của map/HUD. Kế hoạch dùng trạng thái chữ thay cho checkbox.

## Trạng thái hiện tại

| Nhóm | Trạng thái | Còn thiếu chính |
|---|---|---|
| Skill roster | Hiện có, cần kiểm chứng | Cooldown event, failure event, LOS, regression |
| Spell card data/hand/UI | Hiện có | Area target và edge-case validation |
| Spell effects | Hiện có, cần kiểm chứng | Playtest giá trị `Slow`, `Weaken`, `Freeze` và vòng đời status |
| Buff/debuff icon | Hiện có, cần kiểm chứng | Fallback icon và vòng đời UI |
| AI | Một phần | Spell, capture priority, utility score, debug UI |
| Map 1 | Một phần | Gameplay/art/readability chưa nghiệm thu |
| Map 2 | Chưa có | Chưa cần trước khi map 1 ổn định |
| Automated regression | Chưa có | EditMode/PlayMode first-party tests |

## Sprint 1 — Chuẩn hóa skill và spell

### Coder

1. Chốt luật cost, consume, cooldown và thời điểm được dùng.
2. Hoàn thiện `AllAllies`/`AllEnemies` hoặc loại khỏi content phát hành.
3. Nối `SkillEventBus.OnCooldownComplete` khi cooldown về 0.
4. Nối failure event từ action pipeline thay vì chỉ ghi log.
5. Chốt line-of-sight và target chết giữa effect.

### Art/UI

1. Chốt visual language cho heal, defense, control và damage.
2. Chuẩn hóa icon, tooltip, unavailable state và cooldown state.
3. Bảo đảm target highlight và confirm/cancel dễ phân biệt.

### Kết quả cần đạt

- Contract skill/spell thống nhất và không còn data field mơ hồ.
- Invalid target không trừ MP, consume card hoặc kết thúc action sai.
- Mỗi effect đang phát hành có icon/VFX hoặc fallback được duyệt.

## Sprint 2 — Tích hợp và regression

### Coder

1. Test từng skill với hai unit đối địch.
2. Test MP, cooldown, consume và button state.
3. Test status duration, refresh/stack rule và remove.
4. Test caster/target chết, mất reference hoặc rời map giữa sequence.
5. Tạo PlayMode smoke test cho spell và skill flow.

### Art/UI

1. Hoàn thiện cast, release, projectile và impact cue.
2. Kiểm tra VFX spawn point và cleanup qua object pool.
3. Bổ sung combat feedback đủ để hiểu kết quả mà không đọc Console.

### Kết quả cần đạt

- Skill/spell không làm kẹt turn.
- UI quay về trạng thái idle sau success, cancel và failure.
- Không còn missing reference trên content được đưa vào deck mặc định.

## Sprint 3 — Map và chiến thuật

### Gameplay

1. Rà soát spawn point, capture point, choke point và đường tiếp cận.
2. Tạo ít nhất hai lựa chọn di chuyển có trade-off.
3. Kiểm tra melee và ranged đều có vị trí hữu ích.
4. Chốt vật cản ảnh hưởng movement, targeting và LOS.

### Environment

1. Hoàn thiện Đầm Sen Tàn theo palette và chất liệu trong GDD.
2. Phân tách foreground/midground/background, không che grid.
3. Chỉ dựng greybox map 2 khi map 1 đã qua playtest.

### Kết quả cần đạt

- Capture point tạo xung đột thay vì chỉ là đích đi bộ.
- Vật cản nhìn là hiểu và khớp logic map.
- Camera gameplay giữ unit, target và tile luôn đọc được.

## Sprint 4 — AI và polish

### AI

1. Thay random/nearest-only bằng priority rule hoặc utility score nhỏ.
2. Ưu tiên kết liễu, capture, spell có lợi và vị trí không bị kẹt.
3. Có fallback end turn khi không còn action hợp lệ.
4. Thêm decision log, action delay cấu hình và Fast AI.
5. Chạy batch test để phát hiện soft-lock.

### Feedback

1. Indicator cho target/tile AI sắp chọn.
2. Status icon và VFX rõ cho root, stun, shield, heal và damage-over-time.
3. Tối ưu pool và cleanup cho effect lặp lại.

### Kết quả cần đạt

- AI chơi hết trận không soft-lock.
- Người test hiểu action AI và trạng thái combat.
- Frame time không suy giảm liên tục sau nhiều lượt.

## Tiêu chí nghiệm thu Phase 4

### Bắt buộc

- Ít nhất 7 unit có data, prefab và skill phù hợp vai trò.
- Ít nhất 4 spell card hoàn chỉnh về data, target, cost, effect và feedback.
- Buff/debuff tồn tại đúng số lượt và tự kết thúc.
- AI hoàn thành trận local mà không treo lượt.
- Một map hoàn chỉnh về gameplay và readability.
- Có smoke test cho luồng chiến đấu chính.

### Có thể cắt scope

- Map 2 chỉ giữ greybox hoặc hoãn hoàn toàn.
- AI chưa cần search sâu; priority rule hợp lệ là đủ.
- Giảm số spell phát hành thay vì giữ effect chưa hoàn chỉnh.
- Visual độc nhất cho từng effect có thể dùng fallback nhất quán ở bản nội bộ.

## Thứ tự ưu tiên

1. Không kẹt/mất lượt.
2. Skill và spell đúng luật.
3. AI luôn có đường kết thúc lượt.
4. Target/status/UI đọc được.
5. Map, content và polish.
