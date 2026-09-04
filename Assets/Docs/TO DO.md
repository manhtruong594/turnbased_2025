# Backlog

Cập nhật: `2026-09-04`

## P0 — Ổn định gameplay

1. Viết PlayMode smoke test: spawn → move → attack/spell → capture → endgame.
2. Kiểm tra và ngăn double end-turn giữa timer, player và AI.
3. Bỏ workaround `SetTimeFixedTimeInTurn(30f)` trong luồng chờ AI.
4. Hoàn thiện line-of-sight hoặc chốt không dùng LOS.
5. Phát `OnCooldownComplete` khi skill cooldown về 0.
6. Phát skill failure event từ action pipeline.
7. Test target chết/mất reference giữa animation và effect.

## P1 — Spell, AI và UI

1. Hoàn thiện hoặc loại khỏi content các target `AllAllies` và `AllEnemies`.
2. Chốt runtime behavior cho `Slow`, `Weaken` và `Freeze`.
3. Thêm AI priority cho kill, capture point, spell và vị trí an toàn.
4. Thêm decision log và Fast AI cho regression.
5. Hoàn thiện cooldown, unavailable, invalid-target và status feedback.
6. Kiểm chứng Main Menu, Prepare Battle, Inventory và battle navigation.

## P1 — Meta

1. Thiết kế content ID và save format versioned.
2. Lưu profile, Gold, XP, collection và selected deck/spells.
3. Tạo Settings popup và lưu setting.
4. Tạo reward sau trận, chống nhận lặp.
5. Quyết định Shop có thuộc release đầu hay không.

## P2 — Content và polish

1. Cân bằng 7 unit và bộ spell bằng playtest data.
2. Hoàn thiện gameplay/readability của Đầm Sen Tàn.
3. Rà soát animation cue, VFX, SFX, icon và missing reference.
4. Profile hot path, object pool, GC allocation và thời gian tải.
5. Chỉ dựng map 2 sau khi map 1 đạt tiêu chí.

## P3 — Quyết định sản phẩm

1. Chốt nền tảng phát hành đầu tiên.
2. Chốt multiplayer có thuộc release đầu hay không.
3. Nếu có multiplayer, chọn framework và authority model trước implementation.
4. Chốt mô hình kinh doanh trước Shop/IAP/Ads.

## Quy tắc đóng công việc

Một mục chỉ được coi là hoàn thành khi có implementation/asset, tiêu chí nghiệm thu và kết quả kiểm
chứng phù hợp. Compile thành công không thay thế Play Mode test cho gameplay hoặc serialization.
