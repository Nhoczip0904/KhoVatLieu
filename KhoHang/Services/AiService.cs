using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using KhoHang.Models;

namespace KhoHang.Services;

public class AiService
{
    private readonly WarehouseService _warehouseService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public AiService(WarehouseService warehouseService, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _warehouseService = warehouseService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty; // "user" or "model"
        public string Text { get; set; } = string.Empty;
    }

    public class AiSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Persona { get; set; } = "friendly"; // "friendly", "serious", "financial"
        public string UserNickname { get; set; } = "Quản lý";
        public string LanguageStyle { get; set; } = "normal"; // "normal", "concise", "humorous"
    }

    public async Task<string> GenerateResponseAsync(List<ChatMessage> history, AiSettings settings)
    {
        // 1. Resolve API Key (from settings, environment variable, or appsettings.json)
        var apiKey = settings.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = _configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "⚠️ **Thiếu API Key:** Vui lòng cấu hình Gemini API Key trong bảng Cài đặt AI (bấm vào biểu tượng bánh răng bên góc phải hộp thoại) hoặc trong `appsettings.json` để trò chuyện với Trợ lý AI.";
        }

        // 2. Fetch Warehouse Context
        var contextBuilder = new StringBuilder();
        await BuildWarehouseContextAsync(contextBuilder);

        // 3. Define Persona Instructions
        var personaInstruction = GetPersonaInstruction(settings);

        // 4. Build Payload for Gemini
        var client = _httpClientFactory.CreateClient();
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

        var contentsList = new List<object>();

        // We inject the system instruction as the first message or systemInstruction property.
        // Gemini API supports systemInstruction. Let's build the request body accordingly.
        var systemInstructionText = $"{personaInstruction}\n\nĐÂY LÀ DỮ LIỆU KHO HÀNG THỰC TẾ HIỆN TẠI CỦA CỬA HÀNG/DOANH NGHIỆP:\n{contextBuilder}";

        foreach (var msg in history)
        {
            contentsList.Add(new
            {
                role = msg.Role == "user" ? "user" : "model",
                parts = new[] { new { text = msg.Text } }
            });
        }

        var requestBody = new
        {
            contents = contentsList,
            systemInstruction = new
            {
                parts = new[] { new { text = systemInstructionText } }
            },
            generationConfig = new
            {
                temperature = 0.5,
                maxOutputTokens = 1024
            }
        };

        try
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var response = await client.PostAsJsonAsync(url, requestBody, options);

            if (!response.IsSuccessStatusCode)
            {
                var errContent = await response.Content.ReadAsStringAsync();
                return $"❌ **Lỗi từ AI Service:** {response.StatusCode}. Vui lòng kiểm tra lại API Key hoặc kết nối mạng. Chi tiết: {errContent}";
            }

            var jsonResult = await response.Content.ReadFromJsonAsync<GeminiResponse>();
            var text = jsonResult?.Candidates?[0]?.Content?.Parts?[0]?.Text;

            if (string.IsNullOrWhiteSpace(text))
            {
                return "🤖 Xin lỗi, tôi không nhận được câu trả lời hợp lệ từ mô hình AI.";
            }

            return text;
        }
        catch (Exception ex)
        {
            return $"❌ **Lỗi kết nối:** {ex.Message}";
        }
    }

    private async Task BuildWarehouseContextAsync(StringBuilder sb)
    {
        // Stats
        var (revenue, collected, deliveries, activeProjectsCount, recentDeliveries, recentPayments) = await _warehouseService.GetDashboardStatsAsync();
        sb.AppendLine($"--- TỔNG QUAN TÀI CHÍNH & VẬN HÀNH ---");
        sb.AppendLine($"- Doanh thu dự kiến từ các dự án: {revenue:N0} VNĐ");
        sb.AppendLine($"- Tổng tiền thực tế đã thu: {collected:N0} VNĐ");
        sb.AppendLine($"- Công nợ khách hàng còn phải thu (chênh lệch): {revenue - collected:N0} VNĐ");
        sb.AppendLine($"- Tổng số lượt giao hàng (xuất kho): {deliveries}");
        sb.AppendLine($"- Số lượng dự án đang thực hiện (chưa hoàn thành): {activeProjectsCount}");
        sb.AppendLine();

        // Low stock
        var lowStock = await _warehouseService.GetLowStockMaterialsAsync();
        sb.AppendLine($"--- CẢNH BÁO HẾT HÀNG TRONG KHO (StockQty <= MinStockLevel) ---");
        if (lowStock.Any())
        {
            foreach (var m in lowStock)
            {
                sb.AppendLine($"- {m.Name} (Mã: {m.ProductCode ?? "N/A"}): Hiện còn {m.StockQty:0.##} {m.Unit} / Mức tối thiểu: {m.MinStockLevel:0.##} {m.Unit}");
                if (m.Lots != null && m.Lots.Any())
                {
                    var lotInfo = string.Join(", ", m.Lots.Select(l => $"Lô [{l.LotNumber}]: {l.StockQty:0.##}"));
                    sb.AppendLine($"  ↳ Các lô trong kho: {lotInfo}");
                }
            }
        }
        else
        {
            sb.AppendLine("Không có vật tư nào dưới mức tối thiểu. Kho hàng ổn định.");
        }
        sb.AppendLine();

        // Active Projects
        var activeProjects = await _warehouseService.GetProjectsAsync(false);
        sb.AppendLine($"--- DANH SÁCH DỰ ÁN ĐANG CHẠY (Tổng cộng {activeProjects.Count} dự án) ---");
        foreach (var p in activeProjects)
        {
            // Calculate project financial
            decimal projTotal = p.Deliveries.Sum(d => d.TotalAmount);
            decimal projPaid = p.Payments.Sum(pm => pm.Amount);
            decimal projDebt = projTotal - projPaid;

            sb.AppendLine($"- Dự án ID {p.Id}: Khách hàng {p.CustomerName} | SĐT: {p.Phone ?? "N/A"} | Địa chỉ: {p.Address ?? "N/A"}");
            sb.AppendLine($"  ↳ Doanh số đã giao: {projTotal:N0} VNĐ | Đã thu: {projPaid:N0} VNĐ | Còn nợ: {projDebt:N0} VNĐ");

            // Low items in project
            var lowInProject = p.Materials.Where(m => m.RemainingQty <= (m.TotalQty * 0.15) || m.RemainingQty <= 3).ToList();
            if (lowInProject.Any())
            {
                var itemsStr = string.Join(", ", lowInProject.Select(i => $"{i.Name} (Còn {i.RemainingQty:0.##} / {i.TotalQty:0.##} {i.Unit})"));
                sb.AppendLine($"  ↳ Vật tư sắp hết tại công trình: {itemsStr}");
            }
        }
        sb.AppendLine();

        // Suppliers debt summary (approximate from payments and POs)
        var pos = await _warehouseService.GetPurchaseOrdersAsync();
        var supplierPayments = await _warehouseService.GetSupplierPaymentsAsync();

        sb.AppendLine($"--- NHÀ CUNG CẤP & CÔNG NỢ ---");
        var suppliers = await _warehouseService.GetSuppliersAsync();
        foreach (var s in suppliers)
        {
            // Calculate total debt to supplier
            var debt = supplierPayments.Where(sp => sp.SupplierId == s.Id).Sum(sp => sp.Amount);
            // Since debt is stored as negative for purchase (debt) and positive for payment, we invert it to show positive debt
            decimal outstandingDebt = -debt;
            if (outstandingDebt != 0)
            {
                sb.AppendLine($"- NCC {s.Name} | SĐT: {s.Phone ?? "N/A"} | Đang nợ NCC: {outstandingDebt:N0} VNĐ");
            }
        }
    }

    private string GetPersonaInstruction(AiSettings settings)
    {
        var baseInstruction = $"""
            Bạn là một trợ lý AI thông minh tích hợp sẵn trong Hệ thống Quản lý Kho Hàng.
            Bạn đang nói chuyện trực tiếp với {settings.UserNickname} (hãy xưng hô thân mật là "{settings.UserNickname}" và xưng là "Trợ lý AI" hoặc "Em").
            Bạn có quyền truy cập vào thông tin thời gian thực về kho hàng, dự án, công nợ, và dòng tiền của cửa hàng.
            Mục tiêu của bạn là phân tích dữ liệu kho, cung cấp thông tin chính xác, nhanh chóng và đưa ra các đề xuất/gợi ý cá nhân hóa nhằm giúp quản trị viên vận hành hiệu quả nhất.

            Ngôn ngữ: Tiếng Việt.
            Cách trả lời: 
            - Sử dụng Markdown để trình bày đẹp mắt, rõ ràng (in đậm các con số, tên vật tư quan trọng, dùng danh sách bullet point).
            - Trả lời dựa TRÊN DỮ LIỆU THỰC TẾ được cung cấp ở dưới. Nếu dữ liệu không có, hãy lịch sự giải thích là thông tin chưa được cập nhật.
            - Không bịa đặt số liệu.
            - Hãy đề xuất các giải pháp thực tế (ví dụ: khuyên nhập thêm vật tư nào đang sắp hết hàng, nhắc nhở thu hồi nợ dự án nào có công nợ cao, hoặc thanh toán nợ cho nhà cung cấp nào).
            """;

        var personaDetails = settings.Persona switch
        {
            "serious" => """
                [Tính cách: THỦ KHO NGHIÊM TÚC & CHUYÊN NGHIỆP]
                - Trả lời với giọng điệu trang trọng, nghiêm túc, ngắn gọn và đi thẳng vào số liệu kỹ thuật.
                - Tập trung vào tính chính xác của kho bãi, kiểm kê hàng hóa, cảnh báo hao hụt nghiêm ngặt.
                - Tránh đùa cợt hay nói lan man.
                """,
            "financial" => """
                [Tính cách: CỐ VẤN TÀI CHÍNH TỐI ƯU & KINH DOANH]
                - Tập trung phân tích dòng tiền, doanh số dự kiến, công nợ phải thu từ khách hàng, và nợ phải trả cho nhà cung cấp.
                - Đưa ra lời khuyên về tối ưu hóa vốn lưu động (ví dụ: tránh trữ quá nhiều gạch men bán chậm, đề xuất thu hồi nợ khẩn cấp của các công trình nợ lâu).
                - Nhấn mạnh vào lợi nhuận và doanh thu.
                """,
            _ => """
                [Tính cách: TRỢ LÝ THÂN THIỆN & NHIỆT TÌNH]
                - Trả lời thân thiện, cởi mở, dùng một vài biểu tượng cảm xúc (emoji) phù hợp để giao diện trò chuyện sinh động.
                - Luôn sẵn sàng hỗ trợ, giọng điệu động viên và tích cực.
                - Giải thích dễ hiểu, trực quan.
                """
        };

        var styleDetails = settings.LanguageStyle switch
        {
            "concise" => "Hãy trả lời cực kỳ ngắn gọn, tóm tắt các ý chính bằng bullet point, không giải thích dài dòng.",
            "humorous" => "Hãy pha chút hài hước hóm hỉnh phù hợp trong văn phong kinh doanh/kho hàng để tạo cảm giác vui vẻ, thoải mái cho người quản lý.",
            _ => "Trình bày đầy đủ, chi tiết, chuyên nghiệp và có chiều sâu."
        };

        return $"{baseInstruction}\n\n{personaDetails}\n\n{styleDetails}";
    }

    // JSON parsing models for Gemini API response
    private class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }

    private class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }

    private class Content
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; }
    }

    private class Part
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
}
