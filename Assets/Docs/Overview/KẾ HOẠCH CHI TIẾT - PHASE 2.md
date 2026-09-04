# Kế hoạch Phase 2 — Vertical Slice

Tài liệu này ghi lại phạm vi vertical slice và trạng thái hiện tại. Không dùng checkbox; trạng thái
được ghi bằng chữ để phân biệt implementation với kết quả kiểm thử.

## Mục tiêu

Tạo một trận local hoàn chỉnh có hai phe, unit có hình ảnh/animation, turn timer, spawn, movement,
combat, capture point, UI và màn hình kết quả.

## Trạng thái bàn giao

| Hạng mục | Trạng thái | Bằng chứng chính |
|---|---|---|
| Turn state và chuyển phe | Hiện có | `TurnManager`, `PlayerController` |
| Timer theo số unit | Hiện có | `CalculateTimeLimitInTurn` |
| Spawn point theo phe | Hiện có | `SpawnPoint`, `UnitSpawner` |
| MP cost khi spawn | Hiện có | `MPManager`, `UnitSpawner` |
| Movement theo grid/path | Hiện có | `UnitController`, `MapManager`, ProtoTiles |
| Attack, HP và death | Hiện có | `UnitAttack`, `UnitController`, `HealthBar` |
| Capture point và endgame | Hiện có | `CapturePointManager`, `UI_Endgame` |
| Unit roster | Hiện có | 7 unit data/prefab hướng vai trò khác nhau |
| HUD battle | Hiện có, cần kiểm chứng | MP, dice, spawn, skill, spell, endgame |
| Animation/VFX/SFX đầy đủ | Một phần | Có animation/VFX pipeline; độ phủ asset chưa nghiệm thu |
| Vertical-slice smoke test | Chưa có | Chưa có PlayMode test suite first-party |

## Công việc còn lại của Phase 2

### Gameplay

1. Xác nhận timer, manual End Turn và AI completion không cùng gọi chuyển lượt hai lần.
2. Kiểm tra unit chết được unregister khỏi map và không còn trong target list.
3. Hoàn thiện line-of-sight trong `UnitAttack` hoặc xác nhận thiết kế không dùng LOS.
4. Kiểm tra capture point sau move, death và đổi owner.
5. Chạy đủ flow spawn → move → attack → capture → endgame.

### UI/UX

1. Hiển thị lý do spawn/attack/skill không hợp lệ.
2. Bảo đảm button state cập nhật ngay khi MP, turn hoặc selection thay đổi.
3. Kiểm tra HUD ở các resolution mục tiêu.
4. Bổ sung feedback spawn thành công/thất bại nếu asset còn thiếu.

### Art và audio

1. Kiểm tra model, material và animator của toàn bộ unit dùng trong build.
2. Chuẩn hóa màu phe, spawn point, capture point và vùng chọn.
3. Bổ sung SFX/VFX cho spawn, hit, death, capture và endgame còn thiếu.
4. Không để environment/VFX che unit hoặc tile quan trọng.

## Tiêu chí nghiệm thu

- Hai phe chơi hết trận và xác định đúng winner.
- Không exception từ code dự án trong flow chuẩn.
- Invalid action không làm mất MP hoặc action.
- Timer hết chuyển đúng một lượt; UI không còn tương tác với phe cũ.
- Unit, tile, target và capture point đọc được ở camera gameplay.

## Ghi chú phạm vi

Phase 2 không yêu cầu Shop, persistence, multiplayer hoặc content map thứ hai. Các hạng mục đó thuộc
milestone sau và không được dùng để trì hoãn việc ổn định vertical slice.
