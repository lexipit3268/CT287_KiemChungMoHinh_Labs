# ĐỒ ÁN NHỎ: MÁY TÍNH CÁ NHÂN (PERSONAL CALCULATOR)
### Môn học: Kiểm chứng mô hình (Model Checking - CT287)

---

## 1. Giới thiệu tổng quan

Dự án **Máy tính cá nhân (Personal Calculator)** được phát triển bằng ngôn ngữ **C# (.NET 10)** trên nền tảng **WPF (Windows Presentation Foundation)** theo mô hình kiến trúc **MVVM (Model-View-ViewModel)**.

Dự án kết hợp 2 giải thuật cốt lõi nhằm giải quyết bài toán kiểm chứng tính đúng đắn:
1. **Finite State Machine (FSM / Automaton):** Kiểm soát luồng tương tác nhập liệu và ngăn chặn trạng thái lỗi từ gốc (chống nhập 2 dấu chấm thập phân, chống toán tử liên tiếp không hợp lệ, kiểm soát số lượng ngoặc mở/đóng).
2. **Expression Evaluator (Shunting-yard Algorithm & RPN):** Phân giải cú pháp và tính toán giá trị biểu thức toán học phức tạp, hỗ trợ độ ưu tiên toán tử, dấu ngoặc lồng nhau và các hàm toán học khoa học (lượng giác, logarit, căn bậc hai, giai thừa, lũy thừa).

---

## 2. Các chế độ hoạt động & Tính năng (Theo chuẩn Windows 11 Calculator)

### A. Chế độ Tiêu chuẩn (Standard Mode - Grid 4x6)
- Bố cục phím chuẩn xác 1:1 giống Windows Calculator:
  - **Hàng 1:** `%` | `CE` | `C` | `⌫` (Backspace)
  - **Hàng 2:** `¹/x` | `x²` | `²√x` | `÷`
  - **Hàng 3:** `7` | `8` | `9` | `×`
  - **Hàng 4:** `4` | `5` | `6` | `−`
  - **Hàng 5:** `1` | `2` | `3` | `+`
  - **Hàng 6:** `+/-` | `0` | `.` | `=` (Màu Cyber Cyan ấn tượng)
- **Hàng bộ nhớ (Memory Row):** `MC`, `MR`, `M+`, `M-`, `MS`, `M⌄` (tự động làm mờ khi chưa có giá trị lưu trữ).

### B. Chế độ Khoa học (Scientific Mode - Grid 5x7)
- Bố cục phím khoa học 5 cột x 7 hàng:
  - **Hàng 1:** `2ⁿᵈ` | `π` | `e` | `C` | `⌫`
  - **Hàng 2:** `x²` (hoặc `x³`) | `¹/x` | `|x|` | `exp` | `mod`
  - **Hàng 3:** `²√x` (hoặc `∛x`) | `(` | `)` | `n!` | `÷`
  - **Hàng 4:** `xʸ` | `7` | `8` | `9` | `×`
  - **Hàng 5:** `10ˣ` (hoặc `2ˣ`) | `4` | `5` | `6` | `−`
  - **Hàng 6:** `log` (hoặc `log₂`) | `1` | `2` | `3` | `+`
  - **Hàng 7:** `ln` (hoặc `eˣ`) | `+/-` | `0` | `.` | `=`
- **Tùy chọn khoa học:**
  - Nút chuyển đổi góc: **`DEG`** $\leftrightarrow$ **`RAD`**.
  - Nút chuyển đổi định dạng khoa học: **`F-E`** (Fixed-to-Exponential).
  - Menu thả xuống **`⊿ Trigonometry ⌄`**: $\sin, \cos, \tan, \sec, \csc, \cot, \arcsin, \arccos, \arctan$.
  - Menu thả xuống **`𝑓 Function ⌄`**: $|x|, \lfloor x \rfloor, \lceil x \rceil, \exp$.
  - Phím **`2ⁿᵈ`**: Đổi các hàm lũy thừa và căn sang bậc 3 và cơ số 2.

### C. Menu điều hướng (Navigation Drawer - Hamburger `☰`)
- Bấm vào biểu tượng `☰` ở góc trên cùng bên trái để mở Drawer menu chuẩn:
  - Tùy chọn **Standard** và **Scientific** với vạch sáng màu Cyber Neon Cyan chỉ báo chế độ đang kích hoạt.
  - Các mục chế độ khác (Graphing, Programmer, Converter) được gắn nhãn sắp ra mắt.
  - Nhấp vào khoảng mờ bên ngoài để đóng menu trượt.

### D. Tính năng bổ trợ & Kiểm chứng mô hình (CT287)
- **Bảng màu chủ đạo (Sapphire Blue `#1f5ca9`):** Phím `=` và các vạch chỉ báo trạng thái kích hoạt sử dụng tông xanh **#1f5ca9** sang trọng, chữ trắng đậm tương phản cao.
- **Menu điều hướng căn đều bên trái (Left-aligned Drawer):** Toàn bộ icon và tên chế độ (`Standard`, `Scientific`, `Graphing`,...) được căn đều tăm tắp về bên trái dọc theo trục thẳng hàng.
- **Khay lịch sử phục hồi phép tính (Full Calculation Recall):** Khi bấm vào bất kỳ mục lịch sử nào, toàn bộ biểu thức và kết quả cũ sẽ được khôi phục lên màn hình (`ExpressionDisplay` và `Display`), cho phép người dùng tiếp tục tính toán với phép tính đó.
- **Huy hiệu FSM State (`● S0: READY`, `● S1: NUM`,...):** Hiển thị trực quan trạng thái của Automaton ngay trên đầu cửa sổ.

---

## 3. Kiến trúc Hệ thống

```
05_Calculator/
├── Calculator.slnx                       # File Solution (.NET 10)
├── start.ps1                             # Script khởi chạy ứng dụng 1-click
├── Calculator.Core/                      # Thư viện Logic độc lập (100% C# thuần)
│   ├── Enums/
│   │   └── CalculatorEnums.cs            # CalculatorState, CalculatorMode, AngleUnit, CalculatorInputType
│   ├── Models/
│   │   └── CalculatorModels.cs           # Token, CalculationResult, HistoryItem, StateTransitionRecord
│   ├── StateMachine/
│   │   └── CalculatorStateMachine.cs     # Bộ điều khiển chuyển trạng thái hữu hạn (FSM)
│   ├── Parser/
│   │   └── ExpressionEvaluator.cs        # Shunting-yard (Infix -> RPN) & Bộ lượng giá toán học
│   └── Services/
│       ├── MemoryService.cs              # Quản lý bộ nhớ MC, MR, M+, M-, MS
│       ├── HistoryService.cs             # Quản lý danh sách lịch sử tính toán
│       └── CalculatorEngine.cs           # Facade điều phối toàn bộ hệ thống
├── Calculator.UI/                        # Giao diện người dùng WPF (.NET 10 + MVVM)
│   ├── Common/
│   │   ├── RelayCommand.cs               # Cài đặt ICommand chuẩn MVVM
│   │   └── ViewModelBase.cs              # Cài đặt INotifyPropertyChanged
│   ├── ViewModels/
│   │   └── MainViewModel.cs              # ViewModel điều khiển giao diện & phím tắt
│   ├── Views/
│   │   ├── MainWindow.xaml               # Thiết kế XAML hiện đại (Dark theme Fluent)
│   │   └── MainWindow.xaml.cs            # Code-behind bắt sự kiện bàn phím
│   └── App.xaml & App.xaml.cs            # Cấu hình Style, Resource, Start window
└── Calculator.Tests/                     # Bộ kiểm thử tự động (MSTest)
    ├── StateMachineTests.cs              # Kiểm thử chuyển trạng thái & kiểm soát lỗi nhập
    ├── ExpressionEvaluatorTests.cs       # Kiểm thử độ ưu tiên toán tử, ngoặc, hàm khoa học
    ├── MemoryAndHistoryTests.cs          # Kiểm thử bộ nhớ & lịch sử
    └── CalculatorEngineTests.cs          # Kiểm thử tích hợp chu trình tính toán
```

---

## 4. Mô hình Trạng thái Hữu hạn (Finite State Machine - FSM)

Máy trạng thái được thiết kế với các trạng thái chính:

```
[S0: Start] ------------ (Nhập chữ số) -----------> [S1: InputOperand]
     |                                                      |
     |                                                  (Toán tử)
     |                                                      v
     v                                              [S2: OperatorEntered]
(Mở ngoặc '(')                                              |
     |                                                  (Nhập số)
     v                                                      v
[S3: OpenParen] -------- (Nhập số) --------------> [S1: InputOperand]
                                                            |
                                                        (Đóng ngoặc ')')
                                                            v
                                                    [S4: CloseParen]
                                                            |
                                                         (Dấu '=')
                                                            v
                                                    [S6: Evaluated]
                                                            |
                                                       (Chia cho 0)
                                                            v
                                                    [S7: Error State]
```

### Quy tắc kiểm chứng:
1. **Chống 2 dấu chấm:** Khi đang ở `S1` và đã có dấu chấm, mọi lần bấm `.` tiếp theo đều bị chặn.
2. **Thay thế toán tử:** Khi liên tiếp bấm các toán tử (ví dụ `+` rồi bấm ngay `*`), hệ thống tự động thay thế toán tử cũ bằng toán tử mới, không gây lỗi cú pháp.
3. **Cân bằng dấu ngoặc:** Phím `)` chỉ cho phép bấm khi số ngoặc mở `OpenParenCount > 0`. Khi bấm `=`, hệ thống tự động bổ sung đủ các dấu ngoặc đóng còn thiếu.

---

## 5. Hướng dẫn Chạy ứng dụng & Kiểm thử

### Cách 1: Khởi chạy ứng dụng máy tính (WPF)

Mở PowerShell tại thư mục `05_Calculator`:

```powershell
.\start.ps1
```

Hoặc dùng lệnh dotnet:

```powershell
dotnet run --project .\Calculator.UI\Calculator.UI.csproj
```

### Cách 2: Chạy bộ Kiểm thử tự động (Unit Tests)

```powershell
dotnet test .\Calculator.Tests\Calculator.Tests.csproj
```

**Kết quả kiểm thử:**
- **Tổng số test cases:** 23/23 tests.
- **Tỷ lệ đạt:** 100% Passed.
- Bao phủ đầy đủ: State transitions, Operator precedence, Left-associativity, Exponentiation right-associativity, Trigonometry (Deg & Rad), Division by zero, Memory & History.
