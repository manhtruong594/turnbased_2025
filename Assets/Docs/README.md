# Tài liệu dự án

Project: **The Summoners: Ordain and Abyss**  
Engine: Unity `6000.3.9f1`  
Cập nhật: `2026-09-04`

## Cách đọc tài liệu

Tài liệu dùng bốn trạng thái:

- **Hiện có**: đã thấy implementation hoặc asset tương ứng trong project.
- **Một phần**: đã có nền tảng nhưng chưa đủ tiêu chí nghiệm thu.
- **Chưa có**: chưa thấy implementation trong phạm vi kiểm tra.
- **Cần kiểm chứng**: có code/asset nhưng chưa được xác nhận bằng Play Mode hoặc build.

Trạng thái trên không thay thế kiểm thử. Code là nguồn tham chiếu cho implementation; tài liệu thiết
kế là nguồn tham chiếu cho ý định sản phẩm.

## Tài liệu tổng quan

- [Tài liệu thiết kế game](Overview/TÀI%20LIỆU%20THIẾT%20KẾ%20GAME.md): tầm nhìn, luật chơi,
  nội dung, hình ảnh và phạm vi sản phẩm.
- [Lộ trình xây dựng](Overview/LỘ%20TRÌNH%20XÂY%20DỰNG%20GAME.md): milestone, trạng thái và
  thứ tự ưu tiên.
- [Kế hoạch hoàn thiện multiplayer](Overview/MULTIPLAYER_IMPLEMENTATION_PLAN.md): kiến trúc MVP,
  thứ tự triển khai, tiêu chí nghiệm thu và ma trận kiểm thử mạng.
- [Kế hoạch Phase 2](Overview/KẾ%20HOẠCH%20CHI%20TIẾT%20-%20PHASE%202.md): vertical slice.
- [Kế hoạch Phase 4](Overview/KẾ%20HOẠCH%20CHI%20TIẾT%20-%20PHASE%204.md): content expansion.
- [Kế hoạch UI](UISystem_Plan.md): menu, inventory, prepare battle, shop và persistence.
- [Backlog](TO%20DO.md): công việc còn lại theo mức ưu tiên.

## Tài liệu hệ thống

- [Core Gameloop](Systems%20docs/Core_Gameloop_System.md)
- [Spawn Unit](Systems%20docs/SPAWN_UNIT_SYSTEM.md)
- [Skill System](Systems%20docs/SkillSystem_Documentation.md)
- [Spell Card System](Systems%20docs/SpellCard_System.md)
- [Characters](Systems%20docs/Characters_Overview.md)

## Quy tắc cập nhật

- Không dùng checkbox Markdown trong kế hoạch. Ghi trạng thái bằng chữ.
- Không đánh dấu **Hiện có** chỉ dựa trên tài liệu cũ; phải đối chiếu code hoặc asset.
- Thay đổi hành vi hệ thống phải cập nhật tài liệu kỹ thuật tương ứng.
- Chỉ chuyển mục sang hoàn thành sau khi có tiêu chí và kết quả kiểm chứng.
