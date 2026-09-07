# ADR-001: Network stack và mô hình phiên cho multiplayer MVP

- Trạng thái: Chấp nhận
- Ngày: `2026-09-07`
- Phạm vi: multiplayer `1v1` MVP
- Unity Editor: `6000.3.9f1`

## Bối cảnh

Gameplay hiện dùng `GameObject`/`MonoBehaviour`; `LocalMatchAuthority` đang gom một phần mutation nhưng
command còn giữ Unity object reference. MVP cần hai người chơi qua Internet, không yêu cầu mở port,
dedicated server, public matchmaking hoặc host migration.

## Quyết định

### Stack và phiên

| Thành phần | Quyết định |
|---|---|
| Framework | Netcode for GameObjects `2.13.2` |
| Transport | Unity Transport `2.7.4` |
| Session/Relay | Multiplayer Services `2.3.1`; Sessions + Relay |
| Topology | Listen server, host-authoritative |
| Số người | Đúng `2`: host và một client |
| Nền tảng đầu tiên | Windows x86-64 standalone |
| Kết nối Internet | Relay với `dtls`; không yêu cầu port forwarding |
| Kết nối trực tiếp | Chỉ dùng loopback/LAN cho development và chẩn đoán |

Không dùng standalone Lobby/Relay SDK vì Unity 6 hướng dự án mới sang unified Multiplayer Services
SDK. Không gắn gameplay state trực tiếp vào `NetworkObject`; NGO vận chuyển command, result, delta và
snapshot giữa presentation với authority.

### Authority, reconnect và host migration

- Host xác thực và quyết định turn, deadline, MP, RNG, unit, card, capture point và kết quả trận.
- Client chỉ gửi intent. Prediction nếu thêm sau này không được commit authoritative state.
- Client mất kết nối: đóng băng nhận command cho trận, giữ slot tối đa `30 giây`, xác thực lại đúng
  session player rồi gửi snapshot mới nhất. Hết cửa sổ thì client thua do disconnect.
- Client chủ động rời trận: không có cửa sổ reconnect; xử thua do leave.
- Host mất kết nối hoặc rời trận: kết thúc trận ngay với reason code riêng. MVP không host migration.
- Relay không được coi là nơi lưu gameplay state. Reconnect phụ thuộc host còn sống và snapshot host.

### Mô hình tick và message

- Gameplay là event-driven, không chạy fixed-step deterministic simulation.
- NGO network tick: `30 Hz`. Timer gameplay dùng monotonic deadline của host; client chỉ hiển thị ước
  lượng.
- Command, acknowledgement, command result, state delta và lifecycle message dùng reliable ordered.
- Mỗi accepted command cấp một `ServerSequence` tăng đơn điệu. Client bỏ message cũ/trùng và yêu cầu
  snapshot khi thiếu sequence.
- Transform/animation/VFX không phải authoritative state. Chỉ presentation message không ảnh hưởng
  luật chơi mới được dùng unreliable delivery khi có bằng chứng cần tối ưu.

### Giới hạn payload

| Payload | Giới hạn |
|---|---:|
| Command envelope | `1 KiB` |
| Command result hoặc state delta | `4 KiB` mỗi message |
| Snapshot | `256 KiB` tổng; chia chunk tối đa `4 KiB` |
| Session metadata do game định nghĩa | `1 KiB` tổng |

Authority từ chối length, enum, ID, coordinate hoặc collection count ngoài giới hạn trước khi tạo
state mutation. Snapshot vượt giới hạn là lỗi protocol/content cần sửa, không tự tăng giới hạn runtime.

### Version và tương thích build

- Protocol đầu tiên là `ProtocolVersion = 1` (`ushort`). Mọi handshake phải gửi version trước command.
- Hai peer chỉ vào trận khi cùng `ProtocolVersion`, `GameplayRulesVersion` và `ContentCatalogHash`.
- Khác application patch/build number được phép nếu ba giá trị trên giống nhau.
- Thay wire shape hoặc semantics không tương thích phải tăng `ProtocolVersion`.
- Thay dữ liệu cân bằng/content ảnh hưởng gameplay phải đổi `ContentCatalogHash`; thay luật mà không đổi
  wire shape phải tăng `GameplayRulesVersion`.
- Không có downgrade negotiation trong MVP. Host từ chối với reason code cụ thể.

## Prototype và bằng chứng

Scene độc lập: `Assets/Scenes/MultiplayerPrototype.unity`.

Build từ Unity Editor:

```text
Tools > Multiplayer > Build Windows Prototype
```

Hoặc command line:

```powershell
Unity.exe -batchmode -nographics -quit -projectPath <project> `
  -executeMethod MultiplayerPrototypeBuild.BuildFromCommandLine `
  -mp-output <path>\MultiplayerPrototype.exe
```

Chạy hai process:

```powershell
MultiplayerPrototype.exe -batchmode -nographics -logFile host.log -mp-role host -mp-port 27982
MultiplayerPrototype.exe -batchmode -nographics -logFile client.log -mp-role client -mp-port 27982
```

Kết quả kiểm chứng ngày `2026-09-07` trên Windows x86-64:

```text
[MP-PROTOTYPE] HOST_READY port=27982
[MP-PROTOTYPE] HOST_CLIENT_CONNECTED clientId=1
[MP-PROTOTYPE] HOST_RECEIVED_PING clientId=1
[MP-PROTOTYPE] HOST_RECEIVED_DONE clientId=1
[MP-PROTOTYPE] HOST_PASS: message round-trip và ngắt kết nối sạch.

[MP-PROTOTYPE] CLIENT_CONNECTING port=27982
[MP-PROTOTYPE] CLIENT_CONNECTED clientId=1
[MP-PROTOTYPE] CLIENT_RECEIVED_ACK
[MP-PROTOTYPE] CLIENT_PASS: message round-trip hoàn tất; đang ngắt kết nối sạch.
```

Hai process tự thoát sau handshake; không có exception trong lần chạy đạt. Đây là kiểm chứng transport
loopback, chưa phải kiểm chứng Relay qua hai mạng khác nhau.

## Hệ quả

- Giai đoạn 2 phải tạo DTO/ID độc lập Unity object và handshake tương thích trước khi nối gameplay.
- Giai đoạn 4 phải cung cấp snapshot để reconnect; Sessions/Relay không thay thế snapshot.
- Cần Unity project ID, environment và Authentication trước khi kiểm chứng Relay Internet ở giai đoạn 5.
- Không thêm NGO component vào `SampleScene`, `HUDScene` hoặc `MainMenu` trong giai đoạn này.

## Nguồn chính thức

- NGO: https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.netcode.gameobjects.html
- Multiplayer Services SDK: https://docs.unity.com/mps-sdk/
- Relay server: https://docs.unity.com/relay/relay-servers
- Relay protocol và giới hạn host migration: https://docs.unity.com/relay/relay-message-protocol
