# Skills, Spell Cards và Status Effects

## Skills

| Skill | Class sử dụng | Mô tả |
|---|---|---|
| `Normal Arrow` | Archer | Bắn một mũi tên, gây sát thương bằng 125% sát thương hiện tại của người thi triển lên một kẻ địch. |
| `Multi Strike Arrow` | Archer | Bắn nhiều mũi tên liên tiếp. Mũi đầu gây 120% sát thương; các mũi sau gây sát thương giảm dần và có 25% xác suất gây `Freeze`. |
| `Assassin Bleed` | Assassin | Gây 100% sát thương lên một kẻ địch và gây `Bleed`, khiến mục tiêu nhận 10 sát thương vào đầu mỗi lượt trong 3 lượt. |
| `Shadow Dual Strike` | Assassin | Dịch chuyển đến cạnh mục tiêu và tấn công hai lần. Đòn thứ hai mạnh hơn khi mục tiêu còn ít Máu. Nếu hạ mục tiêu, người thi triển nhận `ShadowStep`. |
| `Mace Smash` | Berserker | Gây sát thương bằng 125% sát thương hiện tại của người thi triển lên một kẻ địch. |
| `Blood Hammer` | Berserker | Gây sát thương lớn lên mục tiêu chính và sát thương lan lên các kẻ địch lân cận, sau đó tiêu hao một phần Máu hiện tại nhưng không thể khiến người thi triển tử trận. Hạ mục tiêu chính sẽ nhận `BloodRage`. |
| `Normal Attack` | Axe Soldier, Knight | Gây sát thương bằng 125% sát thương hiện tại của người thi triển lên một kẻ địch. |
| `Crescent Slash` | Axe Soldier | Xoay rìu lướt theo đường thẳng dài 2 ô, gây sát thương cho mục tiêu trên đường lướt, áp dụng `HealBan` lên mục tiêu đầu tiên và tạo `Shield` cho người thi triển. Nếu mục tiêu đang ở trên capture point, chắc chắn áp dụng `HealBan`. |
| `Holy Sword Stance` | Knight | Chém các kẻ địch trên một đường thẳng dài 3 ô, gây `GuardBreak` lên kẻ địch đầu tiên trúng đòn, sau đó người thi triển nhận `StanceGuard`. |
| `Fireball` | Magician | Thi triển hỏa công, gây 125% sát thương lên tất cả kẻ địch trong phạm vi 2 ô quanh ô mục tiêu. |
| `Fire Seal` | Magician | Triệu hồi một biển lửa, gây sát thương lửa diện rộng, tăng sát thương lên mục tiêu đang bị `Burn`, có thể gây `Burn` và để lại `Burning Ground` trên mặt đất có khả năng gây `Burn`. |
| `Heavy Smash Attack` | Smasher | Bổ một nhát búa chí mạng, gây sát thương bằng 125% sát thương hiện tại của người thi triển lên một kẻ địch. |
| `Earthquake` | Smasher | Dồn xung lực vào cây búa giáng mạnh xuống đất, gây sát thương và đẩy lùi một kẻ địch. Nếu mục tiêu va chạm với địa hình, mục tiêu nhận thêm sát thương và bị `Stun`. |
| `Heal Skill` | Archer, Assassin, Axe Soldier, Berserker, Knight, Magician, Smasher | Hồi Máu cho bản thân bằng 150% sát thương hiện tại. |
| `Quick Slash` | Militia | Chém một đường kiếm nhanh khiến đối phương không kịp trở tay, gây 100% sát thương hiện tại lên một kẻ địch. |
| `Power Strike` | Militia | Dồn sức vào nhát chém quyết định, gây 175% sát thương hiện tại. Sát thương tăng thêm 30% nếu mục tiêu đang có `Shield`. |
| `Bandage` | Militia | Hồi Máu cho bản thân bằng 150% sát thương hiện tại. |
| `Staff Tap` | Herbalist | Vung gậy quật mạnh, gây 80% sát thương hiện tại lên một kẻ địch. |
| `Field Remedy` | Herbalist | Hồi Máu cho bản thân hoặc đồng minh bằng 200% sát thương hiện tại. |
| `Purification` | Herbalist | Xóa toàn bộ debuff trên mục tiêu. Nếu xóa thành công ít nhất một debuff, mục tiêu nhận `Shield` bằng 30% sát thương hiện tại trong 2 lượt. |
| `Verdict Stroke` | Adjudicator | Vung gậy phép tạo ra một luồng năng lượng bóng tối, gây 100% sát thương hiện tại lên một kẻ địch trong tầm 2 ô. |
| `Accusation` | Adjudicator | Bắn một chùm năng lượng bóng tối, gây 100% sát thương hiện tại. Nếu mục tiêu còn sống, mục tiêu nhận `GuardBreak` 20% trong 2 lượt. |
| `Forbidden Seal` | Adjudicator | Tạo một vùng cấm thuật, gây 160% sát thương trong bán kính 1 ô. Mỗi kẻ địch còn sống nhận `Weaken` 25% và có 50% xác suất nhận `HealBan` trong 2 lượt. |

## Spell Cards

| Spell Card | Mô tả |
|---|---|
| `Damage Spell` | Gây 4 sát thương lên một kẻ địch trên toàn bản đồ. |
| `Root Spell` | Gây `Root` lên một kẻ địch trong 2 lượt, khiến mục tiêu không thể di chuyển. |
| `Shield Spell` | Tạo `Shield` cho một đồng minh trong 2 lượt. |
| `Stun Spell` | Gây `Stun` lên một kẻ địch trong 1 lượt, khiến mục tiêu không thể hành động. |
| `Ashen Ultimatum` | Đánh dấu tối đa 2 kẻ địch. Cuối lượt kế tiếp của phe đó, mục tiêu chưa di chuyển nhận 20 sát thương, `Weaken` và `GuardBreak`. |
| `Dawn Absolution` | Xóa `Stun` và `Freeze` khỏi một đồng minh, sau đó hồi 20% Máu tối đa. |
| `Ember Sacrifice` | Hy sinh một đồng minh để nhận 3 MP và gây `Burn` lên các kẻ địch trong phạm vi 1 ô. |
| `Merciful Prison` | Hồi 25 Máu cho một unit, sau đó gây `Root` lên unit đó trong 2 lượt. |
| `Stonewall Rise` | Tạo núi đá không thể vượt qua trên ô trống chỉ định trong 4 lượt. |

## Status Effects

| Status Effect | Mô tả |
|---|---|
| `HealOverTime` | Hồi một lượng Máu cố định vào đầu mỗi lượt. Hiệu ứng hồi Máu bị `HealBan` ngăn chặn. |
| `Shield` | Giảm mỗi lần nhận sát thương đi một lượng cố định sau khi áp dụng các modifier phần trăm. Giá trị giảm sát thương không bị tiêu hao sau khi chặn đòn. |
| `DamageBuff` | Cộng một lượng cố định vào sát thương cơ bản. |
| `BloodRage` | Tăng sát thương gây ra theo phần trăm và tăng 1 tầm di chuyển. |
| `StanceGuard` | Giảm sát thương nhận vào theo phần trăm và miễn nhiễm hiệu ứng dịch chuyển. |
| `ShadowStep` | Khiến unit không thể bị chọn làm mục tiêu trực tiếp, nhưng vẫn có thể chịu hiệu ứng diện rộng. |
| `Burn` | Gây một lượng sát thương cố định vào đầu mỗi lượt. |
| `Poison` | Gây sát thương vào đầu mỗi lượt với lượng sát thương tăng tuyến tính theo số lần đã kích hoạt. |
| `Slow` | Giảm tầm di chuyển theo phần trăm. |
| `Weaken` | Giảm sát thương gây ra theo phần trăm. |
| `Root` | Ngăn unit di chuyển nhưng không trực tiếp ngăn tấn công hoặc dùng kỹ năng. |
| `Stun` | Ngăn unit được chọn, di chuyển, tấn công và dùng kỹ năng. |
| `Freeze` | Ngăn toàn bộ hành động. Đòn sát thương trực tiếp đầu tiên gây thêm 20% sát thương, phá `Freeze` và chuyển thời gian còn lại thành `Slow` 20%. |
| `Bleed` | Gây một lượng sát thương cố định vào đầu mỗi lượt. |
| `GuardBreak` | Tăng sát thương mục tiêu nhận vào theo phần trăm. |
| `HealBan` | Ngăn mọi hiệu ứng hồi Máu. |
| `AshenUltimatum` | Đánh dấu unit để kiểm tra ở cuối lượt của phe sở hữu. Nếu unit không di chuyển, unit nhận sát thương, `Weaken` và `GuardBreak`; sau đó dấu bị xóa. |
