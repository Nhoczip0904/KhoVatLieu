using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using KhoHang.Models;
using Microsoft.JSInterop;

namespace KhoHang.Services;

public class AiService
{
    private readonly WarehouseService _warehouseService;
    private readonly IJSRuntime _jsRuntime;
    private readonly IConfiguration _configuration;

    public AiService(WarehouseService warehouseService, IJSRuntime jsRuntime, IConfiguration configuration)
    {
        _warehouseService = warehouseService;
        _jsRuntime = jsRuntime;
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
        public string Persona { get; set; } = "friendly";
        public string UserNickname { get; set; } = "Quản lý";
        public string LanguageStyle { get; set; } = "normal";
    }

    // Agentic AI: structured action from AI response
    public class AiAction
    {
        public string Type { get; set; } = string.Empty; // "create_project", "record_payment", "add_customer"
        public string Summary { get; set; } = string.Empty; // Human-readable summary
        public Dictionary<string, string> Data { get; set; } = new();
    }

    /// <summary>
    /// Parse action block from AI response text. Returns null if no action found.
    /// AI wraps actions in ```action ... ``` code blocks.
    /// </summary>
    public static AiAction? ParseActionFromResponse(string response)
    {
        if (string.IsNullOrEmpty(response)) return null;

        var actionStart = response.IndexOf("```action");
        if (actionStart < 0) return null;

        var jsonStart = response.IndexOf('{', actionStart);
        var jsonEnd = response.IndexOf("```", jsonStart);
        if (jsonStart < 0 || jsonEnd < 0) return null;

        var jsonStr = response.Substring(jsonStart, jsonEnd - jsonStart).Trim();
        try
        {
            return JsonSerializer.Deserialize<AiAction>(jsonStr, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Remove the action block from the response text for display purposes.
    /// </summary>
    public static string RemoveActionBlock(string response)
    {
        if (string.IsNullOrEmpty(response)) return response;
        var actionStart = response.IndexOf("```action");
        if (actionStart < 0) return response;
        var blockEnd = response.IndexOf("```", actionStart + 9);
        if (blockEnd < 0) return response;
        return (response.Substring(0, actionStart) + response.Substring(blockEnd + 3)).Trim();
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
        var systemInstructionText = $"{personaInstruction}\n\nĐÂY LÀ DỮ LIỆU KHO HÀNG THỰC TẾ HIỆN TẠI CỦA CỬA HÀNG/DOANH NGHIỆP:\n{contextBuilder}";

        var contentsList = new List<object>();
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
            // Gọi API qua JavaScript (trình duyệt) để tránh bị chặn địa lý trên server Azure
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
            var requestBodyJson = JsonSerializer.Serialize(requestBody, options);
            var rawResponse = await _jsRuntime.InvokeAsync<string>("callGeminiApi", apiKey, requestBodyJson);

            var jsonResult = JsonSerializer.Deserialize<GeminiResponse>(rawResponse);
            var text = jsonResult?.Candidates?[0]?.Content?.Parts?[0]?.Text;

            if (string.IsNullOrWhiteSpace(text))
            {
                // Kiểm tra xem có lỗi từ API không
                using var doc = JsonDocument.Parse(rawResponse);
                if (doc.RootElement.TryGetProperty("error", out var errEl))
                {
                    var errMsg = errEl.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "Unknown error";
                    return $"❌ **Lỗi từ AI Service:** {errMsg}";
                }
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
        var baseInstruction = $$"""
            Bạn là một trợ lý AI thông minh tích hợp sẵn trong Hệ thống Quản lý Kho Hàng.
            Bạn đang nói chuyện trực tiếp với {{settings.UserNickname}} (hãy xưng hô thân mật là "{{settings.UserNickname}}" và xưng là "Trợ lý AI" hoặc "Em").
            Bạn có quyền truy cập vào thông tin thời gian thực về kho hàng, dự án, công nợ, và dòng tiền của cửa hàng.
            Mục tiêu của bạn là phân tích dữ liệu kho, cung cấp thông tin chính xác, nhanh chóng và đưa ra các đề xuất/gợi ý cá nhân hóa nhằm giúp quản trị viên vận hành hiệu quả nhất.

            Ngôn ngữ: Tiếng Việt.
            Cách trả lời:
            - Sử dụng Markdown để trình bày đẹp mắt, rõ ràng (in đậm các con số, tên vật tư quan trọng, dùng danh sách bullet point).
            - Trả lời dựa TRÊN DỮ LIỆU THỰC TẾ được cung cấp ở dưới. Nếu dữ liệu không có, hãy lịch sự giải thích là thông tin chưa được cập nhật.
            - Không bịa đặt số liệu.
            - Hãy đề xuất các giải pháp thực tế.

            ## TÍNH NĂNG HÀNH ĐỘNG (AGENTIC)
            Khi người dùng yêu cầu THỰC HIỆN một thao tác (tạo dự án, thêm khách hàng, ghi nhận thanh toán), hãy:
            1. Trả lời xác nhận bằng ngôn ngữ tự nhiên (tóm tắt thông tin sẽ thực hiện)
            2. Kèm theo một khối JSON action ở cuối tin nhắn với format chính xác sau:

            ```action
            {"type": "<action_type>", "summary": "<mô tả ngắn>", "data": {...fields...} }
            ```

            Các action_type hỗ trợ:
            - **create_project**: Tạo dự án mới. Data: {"customerName": "...", "phone": "...", "address": "..."}
            - **add_customer**: Thêm khách hàng. Data: {"name": "...", "phone": "...", "address": "...", "note": "..."}
            - **record_payment**: Ghi nhận thanh toán. Data: {"projectId": "...", "amount": "...", "method": "Tiền mặt/Chuyển khoản", "note": "..."}

            LƯU Ý QUAN TRỌNG:
            - Chỉ tạo action block khi người dùng YÊU CẦU RÕ RÀNG muốn thực hiện thao tác.
            - Nếu thiếu thông tin bắt buộc (tên khách hàng, số tiền...), hãy HỎI LẠI thay vì tự bịa.
            - Nếu người dùng chỉ hỏi thông tin, KHÔNG tạo action block.
            - Với record_payment, nếu người dùng nói tên khách/dự án thay vì ID, hãy tra trong danh sách dự án đang chạy bên dưới để tìm projectId chính xác.
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
