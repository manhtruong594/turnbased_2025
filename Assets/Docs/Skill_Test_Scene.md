# Scene thử character, animation và skill

## Mở scene

Mở `Assets/Scenes/SkillTest.unity` rồi vào Play Mode trong Unity Editor. Scene dùng map 5×5 (25 ô) tại `Assets/MyGame/Maps/SkillTest`. Không cần thêm scene vào Build Settings.

## Điều khiển trong Play Mode

- **Character (Player 1)**: chọn prefab character từ `MatchContentCatalog`. Scene tải lại và spawn character ở ô `(-1, 0, 0)`.
- **Target (Player 2)**: scene không tự spawn target. Chọn prefab trong danh sách, bấm **Đặt target lên tile** rồi bấm tile trống để tạo target. Có thể đổi prefab và đặt nhiều target để thử skill diện rộng. Không đặt được ngoài map hoặc trên tile đã bị chiếm.
- **Chuẩn bị điều kiện**: bấm **Làm caster mất HP** trước skill hồi máu; bấm **Gắn Slow lên caster** trước skill giải debuff. Hai nút chỉ chuẩn bị trạng thái test, không tiêu MP hoặc action. Với skill nhắm bản thân, bấm tile caster ở `(-1, 0, 0)`.
- **Skill**: nút skill ghi MP, range và loại target hợp lệ; Fireball có thể chọn cả ô trống. Khi chọn skill, bảng test nhắc thêm điều kiện riêng cho heal, cleanse, Shadow Dual Strike và Forbidden Seal. Bấm tile mục tiêu để gửi lệnh qua `LocalMatchAuthority.SubmitHumanSkill`. Riêng caster của scene test bỏ qua `CanAct()`, yêu cầu đủ MP và cooldown khi dùng skill; skill không trừ MP. Cooldown vẫn được cập nhật và hiển thị, nhưng không chặn lần dùng tiếp theo. Caster chết, target, range và điều kiện riêng của skill vẫn được kiểm tra. Vùng tô lấy từ đường đi trên map nên chỉ mang tính tham khảo, không bảo đảm tile là target hợp lệ. Skill bị từ chối hiển thị lý do. Nhấn **Esc** để hủy chọn skill hoặc đặt target.
- **Animation riêng**: xem Idle, Move, Hit, Death hoặc animation attack của từng skill mà không áp dụng gameplay effect. Mỗi dòng animation có nút **Ping** bên cạnh để chọn và ping `AnimationClip` tương ứng trong cửa sổ Project của Unity Editor (hỗ trợ cả `AnimatorController` lẫn `AnimatorOverrideController`).
- **Reset test**: tải lại scene trong Play Mode, tạo lại caster, đặt HP và MP đầy theo `MPManager.MaxMP`, đưa cooldown về 0 và xóa toàn bộ target. Giữ lựa chọn character/target. Pool được dọn khi khởi tạo lại để không giữ reference đến VFX của scene cũ.

Bảng test hiển thị HP, MP, cooldown và trạng thái hành động. Một skill thành công vẫn kết thúc action của caster theo gameplay; scene test cho phép tiếp tục gửi skill khi `CanAct()` là false hoặc cooldown lớn hơn 0. Nhấn **Reset test** để tạo lại caster và xóa target. Animation riêng không tiêu MP, đổi cooldown hoặc gây damage. Move chỉ phát animation tại chỗ.

Scene sao chép manager từ `HUDScene`, tắt `PlayerController`, `AIController`, battle HUD, input di chuyển và điều kiện thắng từ capture point. Lượt được giữ ở Player 1, không tự hết giờ. Bảng test dùng UI Toolkit. `MatchContentCatalog` phải chứa character prefab và skill của character đó. Map nhỏ phù hợp kiểm tra skill trong phạm vi gần.

## File triển khai và kiểm chứng

- `Assets/Scripts/UI/SkillTestScene.cs`: bảng test, đặt target, gửi lệnh skill, preview/ping animation và reset.
- `Assets/Scripts/Editor/SkillTestSceneBuilder.cs`: menu **Tools > Turn Based > Create Skill Test Scene** tạo scene bằng Unity serialization; từ chối ghi đè scene hoặc thư mục map đã có.
- Đã kiểm tra compile trong Unity, 25 tile có thể sử dụng, reference manager/UI/pool, không có missing script, không có unit đặt sẵn và 10 character trong catalog có prefab/skill hợp lệ.
- Chưa kiểm chứng Play Mode: tương tác UI, animation/VFX, valid/invalid target, MP/cooldown và reset trong runtime.
