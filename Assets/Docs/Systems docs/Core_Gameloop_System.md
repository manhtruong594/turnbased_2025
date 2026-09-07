# Core Gameloop System

## 1. Phạm vi

Tài liệu mô tả luồng runtime của trận local: khởi tạo manager, đổi lượt, thao tác unit, spell card,
capture point và kết thúc trận. Trạng thái được đối chiếu với code tại `Assets/Scripts`.

## 2. Thành phần

| Thành phần | Trách nhiệm |
|---|---|
| `GameMediator` | Phát event giữa turn, MP, unit, capture point, spell và endgame |
| `TurnManager` | Giữ `TurnState`, phe hiện tại, timer, turn count và winner |
| `PlayerController` | Kết nối turn event với input, UI, unit của phe và AI opponent |
| `AIController` | Thực hiện lượt bot theo coroutine |
| `MPManager` | Quản lý MP theo `PlayerID` và thông báo thay đổi |
| `UnitSpawner` | Spawn unit, kiểm tra MP/spawn point và theo dõi roster runtime |
| `MapManager` | Đăng ký unit theo grid, truy vấn tile/unit/range/distance |
| `CapturePointManager` | Theo dõi owner và kiểm tra điều kiện chiến thắng |
| `SpellCardManager` | Quản lý hand, selection, targeting, confirm và use card |
| `ObjectPoolManager` | Tái sử dụng VFX/projectile/UI object được pool |
| `LocalMatchAuthority` | Nhận và xác thực command local trước khi manager thay đổi gameplay state |

Các manager kế thừa `BaseManager` và nhận `GameMediator`/map cache theo bootstrap hiện có. Không tạo
singleton hoặc event channel mới nếu manager hiện tại đã sở hữu trách nhiệm đó.

## 3. State model

### Turn

`TurnState` gồm trạng thái khởi tạo, lượt Player 1, lượt Player 2 và kết thúc game. `TurnManager` là
nguồn chính cho `CurrentState`, `CurrentPlayer`, `TurnCount`, `Timer` và `Winner`.

```text
Initialization
    └── Player1Turn hoặc Player2Turn
            ├── Player1Turn <──> Player2Turn
            └── GameEnd
```

### Unit

`UnitRuntimeStats` giữ owner, map, HP, grid position và cờ hoàn thành move/action. `UnitController`
điều phối movement, skill, damage/heal, buff/debuff và reset theo turn.

### Spell card

`SpellCardManager` dùng state object:

```text
Idle ── chọn card ──> Targeting ── target hợp lệ ──> Confirming
 ^                         │                              │
 └──────── cancel ─────────┴──────── confirm/cancel ─────┘
```

## 4. Luồng khởi tạo

1. Scene tạo các manager và reference được serialize.
2. Manager nhận `GameMediator` và map liên quan.
3. `TurnManager.Initialize` đặt turn count, starting player và `Initialization`.
4. Sau `TurnTransitionDelay`, manager chuyển sang turn state của phe bắt đầu.
5. `GameMediator.NotifyPlayerTurnStarted` kích hoạt controller/UI của phe tương ứng.

Initialization order phụ thuộc reference trong scene. Missing manager hoặc subscribe sau event đầu có thể làm hệ
thống không nhận được turn start; cần kiểm tra trong Play Mode sau thay đổi scene/bootstrap.

## 5. Luồng một lượt

Các entry point local cho `EndTurn`, `SpawnUnit` và `MoveUnit` gửi `IMatchCommand` qua
`LocalMatchAuthority`. Authority kiểm tra `CommandId`, phe đang có lượt và `ExpectedTurn` trước khi
gọi manager sở hữu state. Command trùng, sai phe hoặc thuộc lượt cũ không được thực thi. Đây là seam
để thay bằng network authority sau này; chưa bao gồm transport, lobby hoặc đồng bộ snapshot.

### Bắt đầu lượt

1. `TurnManager.StartPlayerTurn` cập nhật phe và tăng `turnCount`.
2. `PlayerController.HandleTurnStarted` xác định `_isMyTurn`.
3. Phe người chơi reset action, lấy lại danh sách unit và gọi `UnitController.OnTurnBegin`.
4. Timer được tính bằng thời gian nền cộng `unitCount * 10`.
5. UI action/dice/end-turn được bật cho đúng phe.

### Hành động

Người chơi có thể spawn unit, move, dùng attack/skill hoặc spell card khi validation thành công.
Mọi thay đổi MP, unit position, capture point và spell hand phải phát event qua luồng đang có để UI
không giữ state riêng lệch với runtime.

### Kết thúc lượt

1. Manual End Turn, timer hoặc AI gọi `TurnManager.EndCurrentTurn`.
2. `NotifyPlayerTurnEnded` được phát.
3. Unit của phe hoàn tất/reset state cuối lượt.
4. Sau delay, `SwitchToNextPlayer` chuyển state và bắt đầu lượt kế tiếp.

Điểm rủi ro: nhiều nguồn có thể gọi End Turn. Luồng cần guard chống schedule `SwitchToNextPlayer`
nhiều lần trong cùng một turn.

## 6. Lượt AI

Implementation hiện tại:

1. Chờ `actionDelay`.
2. Cộng 3 MP cho AI.
3. Đếm unit còn sống, sau đó spawn unit có utility chiến đấu cao nhất mà MP hiện tại chi trả được nếu chưa đạt `maxUnit`.
4. Gọi `OnTurnBegin` cho unit AI để reset action, giảm cooldown và tick hiệu ứng.
5. Nếu có thể attack, ưu tiên đòn kết liễu rồi đến mục tiêu có phần trăm HP thấp hơn.
6. Nếu chưa thể attack, chấm utility cho các tile đi được theo thứ tự: tạo cơ hội kết liễu/attack,
   chiếm hoặc tiến gần cứ điểm chưa thuộc AI, sau đó áp sát opponent.
7. Sau khi di chuyển, đánh giá lại mục tiêu hợp lệ; chỉ tiếp tục khi lượt hiện tại vẫn thuộc AI.
8. Finish action cho unit và kết thúc lượt.

Giới hạn hiện tại:

- Không dùng spell card.
- Utility hiện là heuristic cố định, chưa có difficulty profile hoặc mô phỏng nhiều lượt.
- AI mới dùng normal attack; chưa chọn skill chủ động theo cooldown/MP/area effect.
- `PlayerController` đặt timer 30 giây như workaround khi chờ AI.
- Chưa có batch regression chứng minh AI không soft-lock.

## 7. Capture và endgame

`CapturePoint` giữ grid position và owner. `CapturePointManager` nghe unit movement/capture event,
cập nhật quyền sở hữu và gọi game end khi đạt luật hiện tại. `TurnManager.TriggerGameEnd` khóa timer,
hủy invoke đang chờ và chuyển sang `GameEnd`. UI nhận winner qua mediator.

Mọi thay đổi luật thắng phải kiểm tra đồng thời capture khi move, unit death, đổi turn và scene restart.

## 8. Event chính

`GameMediator` hiện phát các event cho:

- Player turn started/ended.
- MP changed.
- Unit spawned, selected, deselected và moved.
- Capture point captured.
- Spell card used và hand changed.
- Game ended.

Subscriber phải unsubscribe trong `OnDisable`/`OnDestroy` tương ứng với lifecycle subscribe. Không dùng
event để giữ object đã chết lâu hơn scene.

## 9. Điểm cần hoàn thiện

| Mức | Hạng mục |
|---|---|
| P0 | Guard double end-turn và coroutine/invoke cũ |
| P0 | Smoke test toàn bộ trận local |
| P0 | Xử lý target/unit chết giữa action |
| P1 | Bỏ timer workaround của AI |
| P1 | AI chọn skill chủ động và spell theo utility |
| P1 | Đồng bộ failure feedback cho UI |
| P2 | Automated EditMode/PlayMode regression |

## 10. Kiểm chứng tối thiểu

1. Start đúng phe được cấu hình.
2. Manual end, timeout và AI completion mỗi trường hợp chỉ đổi một lượt.
3. Unit mới spawn tham gia timer và turn reset đúng.
4. Unit chết không còn trong map lookup/target list.
5. Capture đủ điều kiện chỉ phát một winner.
6. Restart scene xóa subscriber/coroutine/state cũ.
