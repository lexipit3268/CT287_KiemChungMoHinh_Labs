# ĐỒ ÁN NHỎ: MÁY TÍNH CÁ NHÂN & BỘ TRỰC QUAN HÓA CÂY BIỂU THỨC
### Môn học: Kiểm chứng mô hình (Model Checking - CT287)
**Khoa Công nghệ Thông tin & Truyền thông — Trường Đại học Cần Thơ (CTU)**

---

## 1. Giới thiệu tổng quan

Dự án **Máy tính cá nhân & Bộ trực quan hóa Cây Biểu Thức (Personal Calculator & Expression Tree Visualizer)** được phát triển bằng ngôn ngữ **C# (.NET 10)** trên nền tảng **WPF (Windows Presentation Foundation)** theo mô hình kiến trúc chuẩn **MVVM (Model-View-ViewModel)**.

Dự án kết hợp chặt chẽ giữa ứng dụng thực tiễn với nền tảng lý thuyết môn **Kiểm chứng mô hình (CT287)** và kế thừa trực tiếp từ bài thực hành **Session 5 - Expression**:
1. **Automata Visualizer Modal (Sơ đồ DFA & NFA - CT287) 🔄:**
   - **Tab 1: 🔄 DFA Máy tính (8 Trạng thái Điều khiển):** $M = (Q, \Sigma, \delta, S_0, F)$. Vẽ đồ thị 8 trạng thái ($S_0 \dots S_7$) với vòng đôi cho trạng thái chấp nhận $S_6$, cung chuyển tiếp có nhãn và **Real-time Active State Glow** (nút trạng thái phát sáng màu Sapphire Blue thời gian thực khi bấm phím trên máy tính).
   - **Tab 2: ⚡ NFA Nhận dạng Số thực (Lexer Tokenizer NFA):** Minh họa mô hình NFA nhận dạng hằng số số thực với các bước chuyển $\epsilon$ và phương pháp tập con (Subset Construction).
2. **Expression Tree Visualizer Modal (Kế thừa Session 5 - Expression) 🌳:**
   - Modal hiển thị cây biểu thức trực quan dạng 2D Canvas có thể cuộn linh hoạt.
   - Hỗ trợ 2 chế độ cây: **🌿 Parse Tree (E $\to$ T $\to$ F)** (ngữ pháp V2 của môn học) và **⚡ AST Tree** (Cây toán tử rút gọn từ Shunting-Yard).
   - Tự động đo đạc và bố trí tọa độ $(X, Y)$ các nút, vẽ các nhánh nối phân cấp rõ ràng.
3. **Slide Thuyết Trình Lướt Ngang (Pure HTML/CSS/JS Presentation Deck):**
   - 10 slide học thuật chất lượng cao, thiết kế Dark Mode Cyber Sapphire Blue, hiệu ứng lướt ngang mượt mà kiểu phân trang/carousel, hỗ trợ phím tắt, thanh tiến trình và điều khiển toàn màn hình. Khởi chạy 1-click qua `start-presentation.bat`.
4. **Programmer Mode (Chế độ Lập trình viên):** Tính toán trên 4 hệ cơ số (HEX, DEC, OCT, BIN) với bảng chữ cái đầu vào động $\Sigma_{\text{base}}$, mặt nạ từ nhớ (Qword, Dword, Word, Byte) và các phép toán logic bitwise.
5. **Giao diện chuẩn Windows 11 Modern UI:** Tông màu điểm nhấn Sapphire Blue **`#1f5ca9`**, hệ thống icon vector trắng thuần, drawer menu căn lề trái chuẩn xác, khung chọn History toàn chiều rộng và thuật toán tự động co giãn kích thước chữ khi nhập số lớn (+20 chữ số).

---

## 2. Các chế độ hoạt động (3 Chế độ Hoàn chỉnh)

### A. Chế độ Tiêu chuẩn (Standard Mode - Grid 4x6)
- Bố cục phím chuẩn xác 1:1 giống Windows Calculator:
  - **Hàng 1:** `%` | `CE` | `C` | `⌫` (Backspace)
  - **Hàng 2:** `¹/x` | `x²` | `²√x` | `÷`
  - **Hàng 3:** `7` | `8` | `9` | `×`
  - **Hàng 4:** `4` | `5` | `6` | `−`
  - **Hàng 5:** `1` | `2` | `3` | `+`
  - **Hàng 6:** `+/-` | `0` | `.` | `=` (Màu Sapphire Blue `#1f5ca9`)
- **Nhập số lớn (Large Number Input):** Cho phép nhập chuỗi số lên đến 45 chữ số với thuật toán tự động thu nhỏ cỡ chữ (*Dynamic Font Downsizing*) kết hợp căn phải, không bị tràn hay cắt dòng.

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

### C. Chế độ Lập trình viên (Programmer Mode - Grid 6x6)
- **Hiển thị đồng thời 4 cơ số:** HEX, DEC, OCT, BIN (nhấp vào bất kỳ hàng nào để đổi cơ số nhập liệu).
- **Lọc bảng chữ cái đầu vào ($\Sigma_{\text{base}}$):** Vô hiệu hóa tự động các phím không thuộc cơ số hiện hành (ví dụ: ở BIN chỉ gõ được 0 và 1; ở HEX gõ được 0-9 và A-F).
- **Mặt nạ từ nhớ (Word Size):** Hỗ trợ QWORD (64-bit), DWORD (32-bit), WORD (16-bit), BYTE (8-bit).
- **Phép toán Bitwise:** `AND`, `OR`, `XOR`, `NOT`, `Lsh` (`<<`), `Rsh` (`>>`), `Mod`.

---

## 3. Bộ Trực quan hóa Cây Biểu thức (Expression Tree Visualizer Modal 🌳)

Nhấp vào nút **`🌳`** trên thanh tiêu đề hoặc chọn mục **`🌳 Parse Tree & AST`** trong Drawer Menu để mở Modal:

1. **🌿 Parse Tree Mode (Cây ngữ pháp E $\to$ T $\to$ F):**
   - Phân giải biểu thức theo cấu trúc ngữ pháp đệ quy V2 từ Session 5:
     $$E \to T ((+ \mid -) T)^*$$
     $$T \to F ((* \mid /) F)^*$$
     $$F \to (E) \mid \text{id}$$
   - Hiển thị đầy đủ cây phát sinh cú pháp với các nút không kết thúc ($E, T, F$) và nút kết thúc (toán tử, số).
2. **⚡ AST Tree Mode (Cây toán tử rút gọn):**
   - Dựng cây cú pháp trừu tượng gọn nhẹ từ kết quả thuật toán Shunting-Yard.
   - Thể hiện rõ thứ tự ưu tiên của các phép toán nhân/chia trước, cộng/trừ sau và xử lý dấu ngoặc.
3. **Thanh thông tin Footer:**
   - Chuỗi biểu thức gốc (`Expression`).
   - Kết quả tính toán (`Result`).
   - Chuỗi ký pháp Ba Lan nghịch đảo (`RPN Trace`).

---

## 4. Kiến trúc Thư mục

```
05_Calculator/
├── Calculator.slnx                       # File Solution (.NET 10)
├── start.bat & start.ps1                 # Script khởi chạy 1-click
├── README.md                             # Tài liệu tổng quan
├── BAO_CAO_DU_AN_CALCULATOR_CT287.md     # Báo cáo học thuật toàn diện chuẩn CTU
├── Calculator.Core/                      # Thư viện Logic độc lập (100% C# thuần)
│   ├── Enums/                            # CalculatorState, CalculatorMode, NumberBase, BitWordSize,...
│   ├── Models/                           # ExpressionTreeNode, ExpressionTreeEdge, ExpressionTreeGraph,...
│   ├── StateMachine/                     # CalculatorStateMachine (FSM phân tích ngữ pháp & trạng thái)
│   ├── Parser/                           # ExpressionEvaluator (Tokenizer, Shunting-yard, AST, RPN)
│   └── Services/
│       ├── ExpressionTreeBuilder.cs      # Xây dựng Parse Tree (E->T->F) và AST với tọa độ 2D
│       ├── ProgrammerEngine.cs           # Xử lý 4 cơ số, mặt nạ bit và phép toán bitwise
│       ├── MemoryService.cs              # Quản lý bộ nhớ MC, MR, M+, M-, MS
│       ├── HistoryService.cs             # Quản lý lịch sử và phục hồi phép tính
│       └── CalculatorEngine.cs           # Facade kết nối toàn bộ hệ thống
├── presentation/                         # Slide thuyết trình thuần HTML5/CSS3/JS kiểu lướt ngang
│   ├── index.html                        # 10 slide học thuật chuẩn mực CT287
│   ├── style.css                         # Giao diện Dark Cyber Sapphire Blue & CSS animation
│   └── presentation.js                   # Điều khiển lướt ngang, bàn phím, vuốt chạm, full-screen
├── start.bat & start.ps1                 # Khởi chạy ứng dụng máy tính 1-click
├── start-presentation.bat                # Khởi chạy Slide thuyết trình 1-click
├── Calculator.UI/                        # Giao diện người dùng WPF (.NET 10 + MVVM)
│   ├── ViewModels/MainViewModel.cs       # ViewModel điều khiển Standard, Scientific, Programmer & 2 Modals
│   ├── Views/MainWindow.xaml             # Giao diện Dark theme, Vector icons trắng, 3 bàn phím & 2 Modals
│   └── App.xaml                          # Bảng màu #1f5ca9, Styles, Templates
└── Calculator.Tests/                     # Bộ kiểm thử tự động MSTest (46/46 PASSED)
    ├── AutomataTests.cs                  # Kiểm thử dựng đồ thị DFA (8 trạng thái) và NFA (từ vựng)
    ├── ExpressionTreeBuilderTests.cs     # Kiểm thử dựng Parse Tree, AST, tọa độ Canvas, RPN
    ├── StateMachineTests.cs              # Kiểm thử FSM, Repeated '=', Dangling Op, chống 2 chấm, cân bằng ngoặc, số lớn 40+ chữ số
    ├── ProgrammerEngineTests.cs          # Kiểm thử chuyển cơ số, chia 0, bảng chữ cái động, bitwise, mask
    ├── ExpressionEvaluatorTests.cs       # Kiểm thử ưu tiên toán tử, ngoặc lồng nhau, hàm khoa học
    ├── MemoryAndHistoryTests.cs          # Kiểm thử bộ nhớ và lịch sử
    └── CalculatorEngineTests.cs          # Kiểm thử tích hợp toàn diện chu trình tính toán
```

---

## 5. Hướng dẫn Khởi chạy và Kiểm thử

### 1. Khởi chạy Ứng dụng Máy tính:
Nhấp đúp vào **`start.bat`** hoặc chạy lệnh PowerShell:
```powershell
.\start.ps1
```

### 2. Khởi chạy Slide Thuyết trình Lướt Ngang:
Nhấp đúp vào **`start-presentation.bat`** hoặc mở trực tiếp:
```
05_Calculator/presentation/index.html
```

### 3. Chạy toàn bộ 46 Kiểm thử tự động (Unit Tests):
```powershell
dotnet test .\05_Calculator\Calculator.Tests\Calculator.Tests.csproj
```
