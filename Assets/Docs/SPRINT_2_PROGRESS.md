# Sprint 2 - Hệ thống MP & Xúc xắc ✅

## 📋 Checklist hoàn thành

### ✅ Mục 1: Hệ thống MP & Xúc xắc

- [x] **Tạo UI cho điểm MP**
  - MPDisplayUI component cho Player 1 & 2
  - Thanh slider với màu sắc thay đổi (đỏ/vàng/xanh)
  - Hiển thị số MP dạng text (current/max)

- [x] **Logic tung 2 xúc xắc và cộng điểm vào thanh MP**
  - Animation tung xúc xắc mượt mà
  - Hiển thị kết quả 2 xúc xắc riêng biệt
  - Tự động tính tổng và cộng vào MP
  - Integration với MPManager

- [x] **Logic lựa chọn hành động từ lượt thứ 2**
  - Lượt 1: Bắt buộc tung 2 xúc xắc
  - Lượt 2+: Ba lựa chọn
    - Tung 2 xúc xắc
    - Tung 1 xúc xắc
    - Bỏ qua không tung
  - UI buttons hiển thị động theo logic lượt

---

## 📦 Các file đã tạo/cập nhật

### Mới tạo:
1. `Assets/Scripts/Resources/MPManager.cs` - Quản lý MP
2. `Assets/Scripts/UI/MPDisplayUI.cs` - Hiển thị thanh MP
3. `Assets/Scripts/TurnSystem/DicePhaseManager.cs` - Quản lý phase tung xúc xắc
4. `Assets/Docs/MP_DICE_SYSTEM_GUIDE.md` - Hướng dẫn chi tiết

### Đã cập nhật:
1. `Assets/Scripts/UI/DiceUI.cs` - Nâng cấp hệ thống xúc xắc
2. `Assets/Scripts/UI/UIManager.cs` - Tích hợp hệ thống mới

---

## 🎮 Hướng dẫn sử dụng nhanh

### 1. Setup Managers (1 lần duy nhất)
```
Hierarchy:
├── GameManagers
│   ├── TurnManager (đã có)
│   ├── MPManager (mới)
│   └── DicePhaseManager (mới)
```

### 2. Setup UI Canvas
```
Canvas
├── PlayerUI
│   ├── Player1Frame
│   │   └── Player1MPDisplay (MPDisplayUI - PlayerID: Player1)
│   └── Player2Frame
│       └── Player2MPDisplay (MPDisplayUI - PlayerID: Player2)
├── DicePanel (DiceUI)
│   ├── Dice1Text (TMP)
│   ├── Dice2Text (TMP)
│   ├── TotalText (TMP)
│   ├── Roll2DiceButton
│   ├── Roll1DiceButton
│   └── SkipDiceButton
```

### 3. Kết nối References

**MPManager:**
- Max MP: `20`
- Starting MP: `0`

**DicePhaseManager:**
- Dice UI: `[Kéo DicePanel vào đây]`

**DiceUI:**
- Dice1 Text, Dice2 Text, Total Text
- Roll2Dice Button, Roll1Dice Button, Skip Dice Button
- Dice Panel: `[Chính nó]`
- Roll Duration: `0.8`
- Roll Interval: `0.06`

**MPDisplayUI (cho mỗi player):**
- Player ID: `Player1` hoặc `Player2`
- Mp Slider: `[Slider component]`
- Mp Text: `[TextMeshPro]`
- Fill Image: `[Slider > Fill Area > Fill]`

---

## 🔧 Code API

### MPManager - Quản lý MP

```csharp
using TurnBasedGame.Resources;
using TurnBasedGame.Core;

// Thêm MP
MPManager.Instance.AddMP(PlayerID.Player1, 5);

// Tiêu tốn MP (trả về bool)
bool success = MPManager.Instance.SpendMP(PlayerID.Player1, 3);

// Kiểm tra đủ MP
bool canAfford = MPManager.Instance.HasEnoughMP(PlayerID.Player1, 5);

// Lấy MP hiện tại
int currentMP = MPManager.Instance.GetCurrentMP(PlayerID.Player1);

// Subscribe event
MPManager.Instance.OnMPChanged += (player, current, max) => {
    Debug.Log($"Player {player}: {current}/{max} MP");
};
```

### DiceUI - Hiển thị xúc xắc

```csharp
// Hiển thị panel (auto gọi từ DicePhaseManager)
diceUI.ShowDicePanel(isFirstTurn: true);  // Lượt 1
diceUI.ShowDicePanel(isFirstTurn: false); // Lượt 2+

// Ẩn panel
diceUI.HideDicePanel();

// Subscribe event khi tung xong
diceUI.OnDiceRolled += (totalValue) => {
    Debug.Log($"Rolled total: {totalValue}");
};
```

### UIManager - Tích hợp

```csharp
// Trong CardUI hoặc action handler
public void OnCardClicked() {
    int cardCost = 3;
    
    // Kiểm tra và tiêu tốn MP
    if (uiManager.HasMP(cardCost)) {
        uiManager.SpendMP(cardCost);
        // Thực hiện hành động
    }
}
```

---

## 🎯 Game Flow

```
1. Lượt bắt đầu
   └─> TurnManager.OnPlayerTurnStarted
       └─> DicePhaseManager.HandleTurnStarted
           └─> DiceUI.ShowDicePanel(isFirstTurn)

2. Player tung xúc xắc
   └─> DiceUI.RollDiceCoroutine
       └─> Animation xúc xắc
       └─> Tính tổng
       └─> MPManager.AddMP(currentPlayer, total)
       └─> DiceUI.OnDiceRolled event
       └─> DiceUI.HideDicePanel()

3. Player thực hiện hành động (spawn, skill,...)
   └─> Check HasMP
   └─> SpendMP
   └─> Execute action

4. End turn
   └─> TurnManager.EndCurrentTurn
```

---

## 🧪 Testing Checklist

### Test Case 1: Lượt đầu tiên
- [ ] Bắt đầu game → Hiện DicePanel
- [ ] Chỉ có nút "Tung 2 xúc xắc"
- [ ] Click tung → Animation chạy
- [ ] Hiện 2 xúc xắc + tổng
- [ ] MP được cộng đúng
- [ ] Panel tự ẩn

### Test Case 2: Lượt 2+
- [ ] Lượt 2 → Hiện 3 nút
- [ ] Tung 2 xúc xắc → Cộng tổng 2 xúc xắc
- [ ] Tung 1 xúc xắc → Chỉ hiện 1 xúc xắc, cộng 1 giá trị
- [ ] Bỏ qua → Không cộng MP

### Test Case 3: MP Validation
- [ ] MP không vượt quá Max (20)
- [ ] Không thể tiêu tốn MP khi không đủ
- [ ] Màu thanh MP đổi theo %:
  - [ ] < 30%: Đỏ
  - [ ] 30-60%: Vàng
  - [ ] > 60%: Xanh

### Test Case 4: Multi-player
- [ ] Player 1 tung → Cộng MP cho Player 1
- [ ] Player 2 tung → Cộng MP cho Player 2
- [ ] MP riêng biệt cho mỗi player

---

## 🐛 Known Issues & Solutions

### Issue: DicePanel không hiện
**Nguyên nhân:** DicePhaseManager chưa có reference  
**Giải pháp:** Kéo DicePanel vào field `Dice UI` trong DicePhaseManager

### Issue: MP không cập nhật
**Nguyên nhân:** MPDisplayUI chưa set đúng PlayerID  
**Giải pháp:** Kiểm tra field `Player ID` trong Inspector

### Issue: Lượt 1 vẫn hiện 3 nút
**Nguyên nhân:** Logic đếm turn sai  
**Giải pháp:** Reset turnCount khi bắt đầu game mới

---

## 📊 Architecture Diagram

```
┌─────────────────┐
│   TurnManager   │ (Singleton)
│   - Quản lý lượt│
└────────┬────────┘
         │ Event: OnPlayerTurnStarted
         ▼
┌─────────────────┐
│DicePhaseManager │
│ - Quản lý phase │
└────────┬────────┘
         │ Calls
         ▼
┌─────────────────┐      ┌─────────────────┐
│     DiceUI      │◄─────┤   UIManager     │
│  - Animation    │      │  - Integration  │
│  - User Input   │      └─────────────────┘
└────────┬────────┘
         │ Calls
         ▼
┌─────────────────┐      ┌─────────────────┐
│   MPManager     │─────►│  MPDisplayUI    │
│  - MP Logic     │Event │  - Hiển thị MP  │
│  - Singleton    │      │  - 2 instances  │
└─────────────────┘      └─────────────────┘
```

---

## 🚀 Next Steps - Sprint 2

### Mục 2: Spawn Unit
- [ ] Tạo hệ thống spawn unit
- [ ] Tiêu tốn MP khi spawn
- [ ] Xác định vị trí spawn hợp lệ
- [ ] Integration với Grid System

### Mục 3: Chiến đấu cơ bản
- [ ] Thêm HP/Damage cho Unit
- [ ] Logic tấn công
- [ ] Unit chết khi HP <= 0

---

## 📚 Tài liệu tham khảo

- `MP_DICE_SYSTEM_GUIDE.md` - Hướng dẫn chi tiết
- `KE_HOACH_SPRINT.md` - Kế hoạch tổng thể
- `Grid_System_Documentation.md` - Hệ thống Grid

---

**Status**: ✅ HOÀN THÀNH  
**Sprint**: 2 - Mục 1  
**Ngày hoàn thành**: October 2025
