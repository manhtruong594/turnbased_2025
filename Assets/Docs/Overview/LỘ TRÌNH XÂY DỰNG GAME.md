# Lộ trình xây dựng game

Cập nhật: `2026-09-07`

## 1. Trạng thái hiện tại

Project đang ở cuối vertical slice local và đầu giai đoạn hoàn thiện content/meta. Gameplay lõi đã có,
nhưng chưa có đủ kiểm thử regression, persistence, Shop/Settings và AI chiến thuật. Multiplayer chưa
được triển khai.

| Hạng mục | Trạng thái | Ghi chú |
|---|---|---|
| Core turn-based loop | Hiện có, cần kiểm chứng | Turn, timer, spawn, move, attack, capture, endgame |
| Unit và skill | Hiện có, cần hoàn thiện | 7 unit, skill data/code; còn event/cooldown/LOS và regression |
| Spell card | Một phần | Hand, MP, target, effect; thiếu area target và một số status runtime |
| AI local | Một phần | Spawn/Move/Attack; thiếu spell, capture priority và utility scoring |
| Main Menu/Prepare/Inventory | Một phần | Các screen lõi có code; cần kiểm chứng scene/prefab flow |
| Shop/Settings/Persistence | Chưa có | Data nền tảng có nhưng thiếu flow hoàn chỉnh và save/load |
| Multiplayer | Một phần | Có command authority local; chưa có runtime networking/lobby/matchmaking |
| Test automation | Chưa có | Có editor test tool thủ công, chưa có test suite first-party |
| Release | Chưa có | Chưa có build artifact và store pipeline |

## 2. Milestone A — Ổn định vertical slice local

Mục tiêu: một trận local có thể chơi từ đầu đến cuối lặp lại mà không soft-lock.

Phạm vi:

1. Hoàn thiện cooldown event, skill failure event và line-of-sight.
2. Kiểm thử từng skill với hai phe, invalid target và target chết giữa effect.
3. Kiểm thử spell card: MP, consume, cancel, buff duration và UI state.
4. Loại bỏ workaround timer chờ AI; bảo đảm mỗi turn chỉ kết thúc một lần.
5. Tạo smoke test PlayMode cho spawn → move → attack/spell → capture → endgame.
6. Sửa lỗi Console thuộc code dự án trong luồng smoke test.

Tiêu chí thoát:

- Chơi liên tiếp tối thiểu 5 trận local không soft-lock.
- Không double end-turn, không mất MP khi target invalid.
- Tất cả skill/spell đang đưa vào deck có feedback và kết thúc action đúng.
- Không có exception từ code dự án trong luồng chuẩn.

## 3. Milestone B — Hoàn thiện AI và combat readability

Mục tiêu: AI đưa ra hành động hợp lệ, có mục đích và quan sát/debug được.

Phạm vi:

1. Priority rule: kết liễu → hành động giá trị cao → capture → tiếp cận → end turn.
2. Dùng spell card khi lợi ích vượt chi phí và target hợp lệ.
3. Tránh tile kẹt; xử lý map không có path hoặc không còn opponent.
4. Decision log, action indicator và tùy chọn Fast AI.
5. Bổ sung tooltip, invalid-action reason, cooldown state và buff/debuff readability.

Tiêu chí thoát:

- AI tự chơi hết 20 trận test mà không treo lượt.
- Log giải thích được action chính.
- Người test nhận biết được target, cost, cooldown và status mà không cần Console.

## 4. Milestone C — Meta flow local

Mục tiêu: hoàn chỉnh vòng lặp ngoài trận và dữ liệu người chơi.

Phạm vi:

1. Main Menu → Prepare Battle/Inventory → Battle → Result → Main Menu.
2. Settings cho âm thanh, đồ họa và điều khiển cơ bản.
3. Save/load profile, Gold, XP, collection và selected deck/spells.
4. Reward sau trận có quy tắc rõ và chống cộng lặp.
5. Shop cơ bản nếu còn thuộc phạm vi sản phẩm.

Tiêu chí thoát:

- Restart app không làm mất profile/deck đã lưu.
- Save migration/fallback không phá dữ liệu cũ.
- Navigation không để lại screen/popup trùng hoặc callback lặp.

## 5. Milestone D — Content và polish

Mục tiêu: một map và roster đủ chất lượng cho demo công khai.

Phạm vi:

1. Cân bằng 7 unit và bộ spell bằng số liệu playtest.
2. Hoàn thiện Đầm Sen Tàn: lane, obstacle, capture point và camera readability.
3. Đồng bộ animation cue, VFX, SFX, icon và status feedback.
4. Tối ưu pool, managed allocation, draw call và thời gian tải.
5. Map thứ hai chỉ làm greybox sau khi map đầu đạt tiêu chí.

Tiêu chí thoát:

- Frame time ổn định trên cấu hình mục tiêu đã chốt.
- Không missing reference/material/script trong scene phát hành.
- Unit/spell sử dụng trong build có asset hoàn chỉnh.

## 6. Milestone E — Multiplayer, tùy chọn

Chỉ bắt đầu sau quyết định sản phẩm và prototype kỹ thuật. Cần chốt authority model, framework mạng,
host/server model và phạm vi reconnect trước khi viết gameplay networking.

Kế hoạch triển khai và tiêu chí nghiệm thu chi tiết nằm tại
[Kế hoạch hoàn thiện multiplayer](MULTIPLAYER_IMPLEMENTATION_PLAN.md).

Phạm vi dự kiến:

- Lobby và matchmaking.
- Đồng bộ turn, command, dice/MP, unit state, skill/spell và capture point.
- Validation phía authority, timeout, disconnect và reconnect.
- Determinism hoặc state reconciliation phù hợp.

Milestone này không chặn bản local nếu multiplayer bị loại khỏi release đầu.

## 7. Milestone F — Release

1. Chốt PC hay Android là nền tảng đầu tiên.
2. Tạo build pipeline, versioning, crash logging và release checklist.
3. Kiểm thử input, resolution, save path và performance trên thiết bị mục tiêu.
4. Chuẩn bị store metadata, privacy policy, icon, screenshot và trailer.
5. Chỉ tích hợp IAP/Ads khi mô hình kinh doanh đã được duyệt.

## 8. Thứ tự ưu tiên

| Ưu tiên | Công việc |
|---|---|
| P0 | Smoke test và sửa lỗi làm kẹt/mất lượt |
| P0 | Hoàn thiện skill/spell đang dùng trong build |
| P1 | AI không soft-lock và có priority hợp lệ |
| P1 | Meta navigation, persistence và settings |
| P2 | Balance, map, VFX/SFX và optimization |
| P3 | Multiplayer hoặc monetization sau quyết định sản phẩm |
