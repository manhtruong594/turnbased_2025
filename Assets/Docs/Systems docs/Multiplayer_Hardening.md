# Độ bền và an toàn multiplayer — Giai đoạn 7

Cập nhật: `2026-10-08`. Implementation đã có; chưa nghiệm thu Unity/Relay.

## Validation và chống spam/replay

- `MatchProtocol`: protocol `2`, gameplay rules `5`. Hai bên cần cùng rules/content/build.
- Command không quá `1024 byte`; enum, canonical lowercase GUID, player và trường theo từng loại
  command được kiểm tra trước executor. Không nhận cost, damage, RNG hoặc target list từ client.
- Coordinate trên từng trục giới hạn `[-1000000, 1000000]` để loại số cực trị trước phép toán grid.
  Executor vẫn kiểm tra tile tồn tại, range, owner, turn, MP, cooldown và card trên state host.
- `SpawnPointId` có dạng canonical `owner:x:y:z`, owner `1` hoặc `2`; chuỗi rỗng/null vẫn có nghĩa
  host tự chọn spawn point theo contract hiện có. Owner ghi trong ID không cấp quyền spawn.
- `CommandId` và client sequence không nhận giá trị cực đại để tránh overflow ở counter kế tiếp.
- `MatchMessageBudget`: burst `20`, hồi `10 message/giây`, dùng monotonic time. Command của mỗi peer
  đã xác thực bị tính phí trước khi đọc/allocate payload. Control handshake/resync có bucket riêng.
  Vượt giới hạn ngắt peer với `MP_COMMAND_RATE_LIMIT` hoặc `MP_CONTROL_RATE_LIMIT`.
- `MatchCommandGate`: giữ `256` kết quả gần nhất, high-water mark cho mỗi player tồn tại suốt trận.
  Replay trong cache nhận kết quả cũ; command ID đã loại khỏi cache bị từ chối, kể cả dùng sequence mới.
  Không thực thi lại. Rematch tạo gate mới; reconnect giữ gate hiện tại.
- Commit mới chỉ broadcast một lần. Reject/replay vẫn nhận ACK riêng.
- Control payload sai không được phép kéo dài deadline bootstrap liên tục hoặc làm host throw do
  chuỗi/enum sai. Kiểm tra peer trước parse và kiểm tra hash canonical.

## Log

`MatchAudit` tạo dòng có `match`, `player`, `command`, `turn`, `sequence`, `reason`.
ID không canonical được thay bằng `unknown`; player/enum lạ được chuẩn hóa. Không đưa token,
service player identity, Relay code, loadout, payload hoặc exception detail vào audit.

Ví dụ:

```text
[MP-AUDIT] match=0123456789abcdef0123456789abcdef player=2 command=8 turn=3 sequence=12 reason=WrongTurn
```

`[MP-RESYNC]` kèm audit ghi sequence client đã apply để đối chiếu log host. Command bị lỗi framing
hoặc chưa parse được dùng command/turn `0`. Kết quả cuối được ghi sau transaction thành công.
Đây là log cục bộ, chưa có telemetry backend.

## Timeout và đóng flow

- `MatchServiceWait`: mỗi lời gọi Authentication/Sessions/Relay có timeout `30 giây`; cancellation
  khi fail/leave/destroy. Task lỗi trả về muộn được observe để không tạo unobserved exception.
- SDK không hỗ trợ hủy mọi request: hủy ở đây chặn continuation của flow, không cam kết dừng request
  phía dịch vụ. Session create/join trả về muộn được leave/delete; lỗi cleanup giữ reference để thử lại.
  Khi request chưa trả về, không cho tạo thêm session trên cùng controller.
- Network handler không khởi động transport nếu flow đã hủy. Chờ shutdown tối đa `5 giây`.
- Lưu ready/start quá hạn đóng flow vì trạng thái dịch vụ có thể chưa xác định; không cho tiếp tục
  bắt đầu trận dựa trên kết quả chưa xác nhận.
- Scene load có timeout `30 giây`; Unity không hỗ trợ cancel `LoadSceneAsync`. Giữ reference thao tác,
  khóa gameplay và chờ load kết thúc trước khi trở về menu/restoring local authority. Nếu vẫn chưa
  xong, hiển thị lỗi và cho thử Rời phòng lại; không giả định scene đã hủy.
- ACK: resync sau `10 giây`, abort sau `30 giây` (giữ implementation trước).
- Reconnect: cửa sổ gốc `30 giây`, không gia hạn do retry; lifetime cancellation dừng continuation
  của request dịch vụ khi cửa sổ hết hạn. Snapshot phải ACK trước khi mở input.

## Kết quả trận và reward

`LocalMatchAuthority.FinalResult` nhận `MatchFinalResult` qua `MatchResultRecorder` sau commit thành công,
bao gồm command người chơi, timeout lượt và forfeit khách. Replica, reject, replay và commit tiếp theo
không tạo kết quả thứ hai. Reset/rematch/clear session xóa bản ghi; reconnect giữ nguyên.

Schema `Version = 1`, `RulesVersion = 5`, `MatchId`, `Winner`, `Turn`, `EndReason`, `ServerSequence`.
`EndReason` giữ semantics state hiện tại (`Value4`), chưa thêm loại kết thúc gameplay mới.

Project chưa có backend lưu kết quả/phát thưởng multiplayer. Bản ghi chỉ nằm trong RAM; không thêm
PlayerPrefs hoặc tự phát tiền/item. Khi có persistence/reward, storage cần unique key `MatchId`,
kiểm tra schema version và transaction idempotent; không dùng event UI/replica làm nguồn phát thưởng.

## Kiểm chứng

Đã chạy ngoài Unity:

- `76/76` case trong `MatchProtocolTests` và `MatchSnapshotTests` đạt bằng Mono/NUnit reflection runner.
- `15` case mới: bucket/refill/clock rollback, SpawnPointId, extreme coordinate/counter,
  replay sau eviction, `5000` payload ngẫu nhiên và byte mutation của cả `9` command,
  authority-only result, log injection, timeout/cancellation và late task fault.
- Compile sơ bộ protocol/runtime/Editor/test bằng Roslyn với reference từ project: `0` lỗi.
  Có warning biến không dùng sẵn có ở `ObjectPoolUsageExample` và `GameplayTestTool`.
- Không chạy Unity Editor, Play Mode, full player build hoặc Relay Internet.

Còn nghiệm thu trong Unity/hai process:

1. Spam command/control gồm payload sai; peer bị giới hạn, không thêm unit/damage/MP/card.
2. Replay trước/sau eviction và reconnect: không double action hoặc kết quả cuối thứ hai.
3. Create/join/ready/start/leave khi dịch vụ treo hoặc trả về sau timeout: không mở transport muộn,
   không giữ Busy vô hạn, cleanup có thể thử lại.
4. Scene tải chậm/quá hạn và Rời phòng: không khôi phục local authority trong scene trận đang tải.
5. ACK mất, hash lệch, resync, disconnect/reconnect: đối chiếu MatchId/sequence/reason hai process.
6. Kết thúc bằng capture và forfeit; rematch tạo MatchId/kết quả mới, client không ghi reward.
