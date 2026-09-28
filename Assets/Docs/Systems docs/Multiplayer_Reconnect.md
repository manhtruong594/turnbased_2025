# Multiplayer — Giai đoạn 6: Timer, disconnect và reconnect

Cập nhật: `2026-09-28`. Đã triển khai code; **chưa nghiệm thu Unity/hai process/Relay Internet**.

## Quy tắc

- Host dùng `NetworkManager.ServerTime.Time` và deadline của `TurnManager`. Client chỉ hiển thị
  thời gian còn lại; không đọc đồng hồ ngày/giờ máy để quyết định action hoặc kết thúc lượt.
- Không gửi action đến hết deadline: host tự `EndTurn`, không tự xử thua do idle. Action mạng đến
  sau deadline bị từ chối, kể cả trước frame cập nhật timer. `EndTurn` vẫn được xử lý qua authority.
- Khách mất kết nối tạm thời: khóa command cả hai bên, giữ slot `30 giây` tính từ lúc host phát hiện.
  Đồng hồ không được cộng lại. Host tiếp tục auto end-turn khi chưa có peer; trong handshake snapshot,
  việc chuyển lượt đợi ACK để snapshot ổn định, rồi xử lý deadline đã hết.
- Hết cửa sổ: host xử khách thua một lần qua `MatchCommandGate.ExecuteSystem`, đóng trận và hiện lý do.
  Không tiêu thụ command ID/client sequence của khách. Nếu đã endgame, giữ nguyên winner.
- Khách chủ động leave hoặc bị loại khỏi membership: host kết thúc trận khi nhận cập nhật membership,
  không chờ hết cửa sổ reconnect. Trong lobby, người dùng có thể rời để tạo/join phòng mới.
- Host rời, session bị xóa, host thay đổi hoặc host phát hiện lỗi Relay: đóng trận; không host migration.
  Client có reason host leave thì đóng ngay. Khi chỉ mất transport, client không thể phân biệt host chết
  với mạng của mình mất tạm thời; thử phục hồi tối đa `30 giây`, rồi đóng flow với `HostLost`.
- Lỗi refresh dịch vụ được hiển thị riêng, không tự thay winner hay biến thành kick.

## Luồng phục hồi

`MatchSessionController` giữ session, danh tính Authentication, proof và launch/loadout cũ.
`MatchReconnectWindow` dùng `Time.realtimeSinceStartupAsDouble`; retry không gia hạn cửa sổ hoặc đổi
chủ slot. Không hỗ trợ khởi động lại ứng dụng/host để phục hồi trận.

1. Khách đợi NGO shutdown hoàn tất, reconnect/refresh membership và kiểm tra host/danh tính.
2. Join lại Relay để lấy allocation mới, dùng `dtls`, rồi khởi động lại NGO trên manager hiện tại.
3. Host refresh membership và xác thực service player ID, proof, build/content và đúng khách của launch.
   Peer cũ còn đang timeout trả `MP_SLOT_BUSY`; khách retry trong cửa sổ cũ. Peer khác không được chiếm slot.
4. Bootstrap giữ scene, MatchId, authority, RNG, replay cache và server sequence. Host bind NGO peer mới,
   gửi snapshot chỉ chứa dữ liệu khách được phép xem. Không reset trận hoặc chạy lại animation cũ.
5. Client bỏ callback/action đang chờ khi transport mất; snapshot khôi phục state và counter command.
   Không tự gửi lại intent chưa biết kết quả. Snapshot/ACK hoàn tất trước deadline mới mở input.

`SessionNetworkHandler` triển khai `INetworkHandler` để Sessions quản lý Relay/membership còn controller
quản lý vòng đời NGO, bao gồm restart transport. Phần lấy Relay join code đọc `_session_network`
(`RelayJoinCode`, `HostId`) theo source Multiplayer Services `2.3.1` đang pin; cần đối chiếu lại khi nâng SDK.
Không ghi proof, Relay code hay allocation vào log.

`MatchDisconnectReason` phân biệt `TemporaryNetwork`, `ClientLeft`, `Kicked`, `HostLost`, `HostLeft`,
`ServiceError`, `ReconnectExpired`, `Rejected`. Đây là trạng thái lifecycle ở session; không thêm backend
reward/persistence. Endgame presentation vẫn dùng guard hiện có để tránh thông báo lặp sau snapshot.
`GameplayRulesVersion` tăng từ `3` lên `4`; hai máy phải dùng cùng build/rules.

## Kiểm chứng

- `83/83` test protocol/gate/snapshot/lobby/reconnect đạt trên Mono ngoài Unity; có `6` case mới cho
  mốc 30 giây, retry không gia hạn, đúng chủ slot, hoàn tất rồi mất kết nối lần nữa, slot chưa tồn tại.
- Thêm `3` test Unity EditMode vào `MatchGameplayAuthorityTests`: action sau deadline không mutate state,
  giữ quy tắc local và forfeit chỉ commit/notify một lần. Chưa chạy các test này trong Unity.
- Compile sơ bộ protocol/runtime/Editor/test bằng Roslyn với reference Unity. Không thay scene/prefab,
  package hay ProjectSettings. Unity tự import script mới và tạo `.meta`.

Ma trận nghiệm thu còn lại:

| Ca | Kết quả cần đạt |
|---|---|
| Đổi giờ/ngày máy client | Deadline host không đổi; client không tự end-turn |
| Không gửi action, gửi sát/sau deadline | Một lần chuyển lượt; action quá hạn không đổi MP/RNG/unit/card |
| Ngắt mạng khách rồi nối trước 30 giây | Snapshot/hash/turn đúng, input mở sau ACK |
| Disconnect sau commit trước ACK | Không lặp damage, spawn, consume card hoặc thông báo endgame |
| Retry nhiều lần, ACK tại/sau 30 giây | Không gia hạn; host xử thua một lần, từ chối slot hết hạn |
| Giả service ID/proof hoặc peer thứ ba | Không bind, không nhận snapshot/hand |
| Khách leave/kick, host leave/crash/lỗi Relay | Lý do phù hợp; input đóng; không chuyển host |
| Lỗi dịch vụ nhưng transport còn sống | Báo lỗi riêng, không tự forfeit |
| Lobby/loading/đang lượt/endgame | Reconnect hoặc thoát flow sạch; giữ winner nếu đã kết thúc |
| Rời trong lúc reconnect/đợi dịch vụ | Kết quả async đến muộn không restart NGO hoặc mở input |
| Reconnect nhiều lần → rematch → leave → local | Không callback trùng, không giữ authority/state trận cũ |

Chưa kiểm tra Unity Console, UI trực quan, Play Mode hoặc hai thiết bị qua Internet. Compile/test thuần
không chứng minh SDK Relay và lifecycle scene hoạt động đúng trên thiết bị.
