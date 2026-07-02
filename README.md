# 📦 Hệ Thống Quản Lý Kho Vật Liệu Xây Dựng HPH

Hệ thống quản lý kho hàng và công nợ vật liệu xây dựng thông minh được tối ưu hóa cho doanh nghiệp phân phối vừa và nhỏ. Dự án được phát triển trên nền tảng **Blazor Server (.NET 9)**, tích hợp **Trợ lý AI Cục bộ (Ollama)** và công cụ dự báo/gợi ý học máy **ML.NET**.

---

## 🚀 Các Tính Năng Đột Phá

### 1. Trợ Lý AI Chat & Agentic Smart Action (Local / Cloud)
*   **Chạy Ngoại Tuyến (Offline)**: Hỗ trợ tích hợp **Ollama (Local LLM)** để đảm bảo dữ liệu kho hàng được bảo mật tuyệt đối, không chia sẻ ra bên ngoài.
*   **Trí tuệ Nhân tạo Đám mây**: Cho phép chuyển đổi nhanh sang **Google Gemini Cloud API**.
*   **Thẻ Hành Động Thông Minh (Smart Action Cards)**: AI không chỉ trả lời câu hỏi mà còn có khả năng tự động phân tích ý định của người dùng để đề xuất các thẻ hành động (nhập kho, thanh toán, xuất phiếu...) hiển thị trực quan để người dùng xác nhận thực thi chỉ với 1-click.

### 2. Dự Báo Nhu Cầu Tiêu Thụ Tồn Kho (Machine Learning)
*   **Thuật toán SSA (Singular Spectrum Analysis)**: Tích hợp công cụ **ML.NET TimeSeries** để phân tích lịch sử giao dịch và tự động dự báo lượng tiêu thụ vật tư trong 15 ngày tiếp theo.
*   **Trực Quan Hóa 3 Chặng**: Biểu đồ cột CSS tinh tế phân đoạn nhu cầu (5 ngày đầu, 5 ngày giữa, 5 ngày cuối).
*   **Cảnh Báo Cạn Kho**: Chủ động so sánh lượng tồn kho hiện tại với nhu cầu dự báo để cảnh báo người dùng chuẩn bị nhập hàng trước khi chạm đáy 0.

### 3. Gợi Ý Vật Tư Đi Kèm Thông Minh (Recommendation Engine)
*   **Khai Thác Mẫu Tuần Hoàn (Co-occurrence Mining)**: Tự động phân tích lịch sử giỏ hàng và danh mục của các dự án trước đó để tìm ra các nhóm vật tư thường xuyên được sử dụng chung (ví dụ: gạch dán tường -> keo dán gạch, xi măng).
*   **Tối Ưu Hóa Quy Trình Xuất Kho**: Tự động gợi ý vật tư đi kèm tương quan nhất ngay trong phiếu dự thảo xuất kho, chỉ đề xuất các mặt hàng có trong hợp đồng dự án để tránh sai sót.

### 4. Nghiệp Vụ Kho Tiêu Chuẩn
*   **Quản lý tiến độ cấp hàng**: Theo dõi hạn mức và số lượng còn lại của từng loại vật tư trong dự án.
*   **Quản lý Lô Hàng (Batching & Inventory Lots)**: Quản lý chi tiết giá vốn nhập, giá bán lẻ, số lượng tồn kho của từng lô riêng biệt.
*   **Báo cáo công nợ đa chiều**: Công nợ khách hàng (chốt nợ lũy tiến, tính cọc/không tính cọc) và công nợ nhà cung cấp.

---

## 🛠️ Công Nghệ Sử Dụng

*   **Framework**: Blazor Server (.NET 9.0)
*   **Database**: SQLite (EF Core - Entity Framework)
*   **Machine Learning**: ML.NET (Microsoft.ML & Microsoft.ML.TimeSeries 3.0.1)
*   **Local AI**: Ollama (OpenAI API-compatible endpoint)
*   **UI/UX**: HTML5, Vanilla CSS (Premium Glassmorphism & Custom Micro-animations), Bootstrap Icons

---

## 💻 Hướng Dẫn Cài Đặt & Chạy Local

### 1. Yêu Cầu Hệ Thống
*   Cài đặt **.NET 9.0 SDK** ([Tải về tại đây](https://dotnet.microsoft.com/download/dotnet/9.0)).
*   Cài đặt **Ollama** ([Tải về tại đây](https://ollama.com)) nếu muốn dùng Local AI.

### 2. Thiết Lập Ollama Cục Bộ
Mở Command Prompt/PowerShell và tải xuống mô hình AI phù hợp (khuyến nghị dùng `qwen2.5:7b` hoặc `llama3` để có kết quả tốt nhất):
```bash
ollama run qwen2.5:7b
```

### 3. Chạy Dự Án
Di chuyển vào thư mục dự án và khởi chạy:
```bash
dotnet run --project KhoHang/KhoHang.csproj
```
Ứng dụng sẽ được mở tại: **`http://localhost:5134`**

### 4. Cấu Hình AI Trên Giao Diện
1. Vào ứng dụng, nhấp vào **Trợ lý AI** ở góc dưới cùng bên phải.
2. Chọn tab **Cá nhân hóa** (Biểu tượng bánh răng ⚙️).
3. Chọn **Ollama Local AI** làm nhà cung cấp. Nhập URL `http://localhost:11434` và Model Name `qwen2.5:7b`.
4. Bấm **Lưu cài đặt** để kích hoạt trợ lý AI ngoại tuyến.

---

## ☁️ Hướng Dẫn Deploy Lên Azure

Dự án đã được cấu hình sẵn pipeline **GitHub Actions** để tự động build và triển khai lên **Azure App Service**.

1. **Publish Profile**: Tải tệp `.publishsettings` từ Azure App Service của bạn.
2. **GitHub Secrets**: Thêm Secret có tên `AZURE_WEBAPP_PUBLISH_PROFILE` trên kho chứa GitHub của bạn và dán nội dung tệp settings vào.
3. **Connection String**: Trên Azure Portal, thêm Environment Variable hoặc Connection String tên là `DefaultConnection` với kiểu `Custom` chỉ định đường dẫn lưu SQLite (ví dụ: `Data Source=d:\home\khohang.db`).
4. **Kích hoạt deploy**: Mỗi lần bạn chạy `git push origin main`, GitHub Actions sẽ tự động thực hiện mọi việc còn lại.
