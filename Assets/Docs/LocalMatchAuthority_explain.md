`LocalMatchAuthority` là cổng duy nhất tiếp nhận ý định gameplay và quyết định command có được phép thay đổi state hay không. Nó phục vụ cả trận local và host-authoritative multiplayer.

## Luồng tổng quát

```text
UI / AI
   ↓ SubmitHumanMove / SubmitSkill / SubmitSpawn...
MatchCommandDto
   ↓
LocalMatchAuthority.SubmitLocal()
   ├─ Client → gửi DTO qua MatchGameplayTransport
   └─ Host/local → MatchCommandGate → Execute()
                                      ↓
                              Gameplay managers
                                      ↓
                    CommandAcknowledgement + state mới
```

### 1. Chuyển thao tác thành DTO

Các API như:

- `SubmitHumanMove`
- `SubmitHumanSkill`
- `SubmitHumanSpell`
- `SubmitHumanSpawn`
- `SubmitHumanEndTurn`

không trực tiếp sửa gameplay state. Chúng gọi `Create()` để tạo `MatchCommandDto`:

```csharp
Compatibility
MatchId
CommandId
Actor
ExpectedTurn
ClientSequence
AcknowledgedServerSequence
Kind
```

Sau đó bổ sung payload riêng như `UnitRuntimeId`, `Destination`, `SkillContentId` hoặc `CardInstanceId`.

Điểm quan trọng: DTO chỉ chứa ID, enum, số và tọa độ; không chứa `GameObject`, `UnitController`, `Transform` hay callback. Vì vậy cùng DTO có thể serialize và gửi qua mạng.

Xem [ICommand.cs:594](E:/UnityProject/My%20Turnbased/turnbased_2025/Assets/Scripts/Manager/ICommand.cs:594) và [MatchProtocol.cs:50](E:/UnityProject/My%20Turnbased/turnbased_2025/Assets/Scripts/Multiplayer/Protocol/MatchProtocol.cs:50).

## 2. Phân nhánh client và authority

`IsAuthoritative` xác định instance hiện tại có quyền thay đổi gameplay state hay không:

- Trận local: mặc định authoritative.
- Host multiplayer: authoritative.
- Remote client: không authoritative.

Tại `SubmitLocal()`:

```csharp
if (!IsAuthoritative)
    return Transport.Send(command, callback);
```

Client chỉ gửi ý định. Nó không tự trừ MP, di chuyển unit, gây damage hoặc đổi lượt.

Nếu là host/local, command được chuyển vào `MatchCommandGate` để xác thực và thực thi.

Xem [ICommand.cs:191](E:/UnityProject/My%20Turnbased/turnbased_2025/Assets/Scripts/Manager/ICommand.cs:191).

## 3. `MatchCommandGate` kiểm tra tính hợp lệ ở cấp protocol

Trước khi chạy gameplay, gate kiểm tra:

- Protocol/rules/content có tương thích không.
- `MatchId` có đúng trận không.
- `command.Actor` có trùng người chơi đã được session xác thực không.
- `CommandId` có bị gửi lại không.
- Cùng `CommandId` có bị tái sử dụng với payload khác không.
- `ClientSequence` có đúng thứ tự không.
- Client có acknowledgement một `ServerSequence` chưa tồn tại không.
- Authority có đang chạy command khác hoặc đã fault không.

Command trùng hoàn toàn sẽ nhận lại kết quả đã cache, không thực thi lần thứ hai. Đây là cơ chế chống double spawn, double damage, double end-turn khi packet bị gửi lại.

Xem [MatchCommandGate.cs:31](E:/UnityProject/My%20Turnbased/turnbased_2025/Assets/Scripts/Multiplayer/Protocol/MatchCommandGate.cs:31).

## 4. `Execute()` kiểm tra gameplay rule

Sau khi qua protocol gate, `Execute()` kiểm tra state thực tế:

### Kiểm tra chung

- Trận không ở `Initialization` hoặc `GameEnd`.
- Không đang chuyển lượt.
- Đúng `CurrentPlayer`.
- Đúng `ExpectedTurn`.
- Không có unit đang chạy animation di chuyển.

### Kiểm tra theo command

Ví dụ `MoveUnit`:

- Unit tồn tại.
- Unit thuộc actor.
- Unit còn sống và có thể di chuyển.
- Tile đích tồn tại và chưa bị chiếm.
- Có path hợp lệ.
- Đích nằm trong move range.

Ví dụ `UseSkill`:

- Source thuộc actor.
- Unit có thể hành động.
- Skill thuộc unit và đúng loại.
- Target tile tồn tại.
- Không cooldown.
- Đủ range và line-of-sight.
- Đủ MP.
- `skill.CanUse()` chấp nhận target.

Chỉ sau tất cả validation, code mới gọi entry point có quyền thay đổi state như:

```csharp
skill.ExecuteAuthorized(...)
unit.TryMoveAuthorized(...)
UnitSpawner.Instance.TrySpawnUnitAuthorized(...)
turn.TryEndCurrentTurn(...)
```

Xem [ICommand.cs:336](E:/UnityProject/My%20Turnbased/turnbased_2025/Assets/Scripts/Manager/ICommand.cs:336).

## 5. Command được xử lý như transaction

Trước khi thực thi, `LocalMatchAuthority` chụp:

- State dùng để tạo kết quả.
- Các hàm rollback của unit, map, MP, spell hand, hazard, capture point, turn.
- Trạng thái RNG và xúc xắc.

Nếu execution ném exception:

1. Gate đánh dấu authority `faulted`.
2. Gọi rollback.
3. Trả `ExecutionFault`.
4. Không tăng `ServerSequence`.
5. Trận bị dừng nhận command tiếp theo để tránh tiếp tục từ state không đáng tin cậy.

Lưu ý: rollback hiện được kích hoạt khi có exception. Một command bị từ chối bình thường phải tự bảo đảm validation diễn ra trước mutation.

Xem [ICommand.cs:227](E:/UnityProject/My%20Turnbased/turnbased_2025/Assets/Scripts/Manager/ICommand.cs:227) và [MatchCommandGate.cs:63](E:/UnityProject/My%20Turnbased/turnbased_2025/Assets/Scripts/Multiplayer/Protocol/MatchCommandGate.cs:63).

## 6. Presentation chỉ chạy sau commit

Trong lúc command chạy, các event presentation được gom vào `presentation`:

```csharp
PublishAfterCommit(() => ...);
```

Chúng chỉ được phát sau khi:

- Command thành công.
- `ServerSequence` đã tăng.
- `CommandCommitted` đã được gửi.

Điều này ngăn UI, animation hoặc event bên ngoài quan sát state đang thực thi dở hay đã rollback.

Xem [ICommand.cs:89](E:/UnityProject/My%20Turnbased/turnbased_2025/Assets/Scripts/Manager/ICommand.cs:89) và [ICommand.cs:218](E:/UnityProject/My%20Turnbased/turnbased_2025/Assets/Scripts/Manager/ICommand.cs:218).

## 7. Kết quả chứa state authoritative

Sau command thành công, `CaptureChanges()` chụp state sau command, gồm:

- MP
- Unit, HP, vị trí, action flags
- Cooldown và status
- Hand card
- Hazard
- Spawn point và capture point
- Turn, deadline, winner
- RNG
- Dice

Unit biến mất được biểu diễn bằng `RemovedUnit`.

`CommandAcknowledgement` chứa:

```csharp
Accepted
Reason
Detail
ServerSequence
NextClientSequence
StateChanges
```

Client dùng kết quả này để cập nhật replica, thay vì tự mô phỏng kết quả command.

Xem [ICommand.cs:256](E:/UnityProject/My%20Turnbased/turnbased_2025/Assets/Scripts/Manager/ICommand.cs:256).

## Vai trò trong kế hoạch multiplayer

Theo `MULTIPLAYER_IMPLEMENTATION_PLAN.md`, class này hiện là nền móng của mô hình:

> Client gửi intent → host xác thực → host thay đổi state → trả kết quả/state.

Tuy nhiên tên `LocalMatchAuthority` hơi dễ gây hiểu nhầm:

- Nó không chỉ dùng cho local match.
- Trong multiplayer, cùng class này chạy trên host để làm authority.
- Trên remote client, nó chủ yếu là adapter đóng gói và gửi command.
- Biên xác thực mạng thật sự còn phụ thuộc `MatchGameplayTransport` và session mapping giữa network peer với `PlayerId`.

`ICommand` ở đầu file là Command Pattern cũ dành cho `Execute()/Undo()` nội bộ. Nó không phải multiplayer command. Multiplayer dùng `MatchCommandDto` + `MatchCommandGate`; chỉ DTO được phép đi qua mạng.