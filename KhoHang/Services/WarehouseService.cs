using Microsoft.EntityFrameworkCore;
using KhoHang.Data;
using KhoHang.Models;
using System.Net;
using System.Net.Sockets;

namespace KhoHang.Services;

public class WarehouseService
{
    private readonly IDbContextFactory<KhoDbContext> _dbFactory;

    public WarehouseService(IDbContextFactory<KhoDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<string>> GetUniqueUnitsAsync()
    {
        using var context = _dbFactory.CreateDbContext();
        var commonUnits = new List<string> { "m", "thùng", "bao", "viên", "bộ", "cái", "kg", "lít", "cuộn", "tấm", "cây", "m2", "m3", "chuyến" };
        var dbUnits = await context.Materials
            .Select(m => m.Unit)
            .Distinct()
            .Where(u => !string.IsNullOrEmpty(u))
            .ToListAsync();

        return commonUnits.Union(dbUnits).OrderBy(u => u).ToList();
    }

    public string GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    string ipStr = ip.ToString();
                    // Avoid virtual adapter IPs if possible (common for VPNs/VMs)
                    if (!ipStr.StartsWith("127.") && !ipStr.StartsWith("169.254.") && !ipStr.StartsWith("26."))
                    {
                        return ipStr;
                    }
                }
            }
        }
        catch { }
        return "localhost";
    }

    public string GetStableHostName()
    {
        return $"{Environment.MachineName.ToLower()}.local";
    }

    public async Task SeedDataAsync()
    {
        using var context = _dbFactory.CreateDbContext();

        // Kiểm tra nếu đã có dữ liệu thì không seed lại nữa để tránh mất dữ liệu người dùng
        if (context.Materials.Any())
        {
            return;
        }

        // Nếu DB trống, tiến hành seed dữ liệu mẫu
        await context.SaveChangesAsync();

        // 1. Create standardized Categories
        var categories = new List<Category>
        {
            new Category { Name = "Gạch men 20x40", Icon = "bi-grid-3x3" },
            new Category { Name = "Gạch men 25x40", Icon = "bi-grid-3x3" },
            new Category { Name = "Gạch men 30x45", Icon = "bi-grid-3x3" },
            new Category { Name = "Gạch men 30x60", Icon = "bi-grid-3x3" },
            new Category { Name = "Gạch men 30x30", Icon = "bi-grid-3x3" },
            new Category { Name = "Gạch men 40x40", Icon = "bi-grid-3x3" },
            new Category { Name = "Gạch men 50x50", Icon = "bi-grid-3x3" },
            new Category { Name = "Gạch men 60x60", Icon = "bi-grid-3x3" },
            new Category { Name = "Gạch men 80x80", Icon = "bi-grid-3x3" },
            new Category { Name = "Gạch men 40x80", Icon = "bi-grid-3x3" },
            new Category { Name = "Gạch xây dựng", Icon = "bi-bricks" },
            new Category { Name = "Cát", Icon = "bi-moisture" },
            new Category { Name = "Đá, Sỏi", Icon = "bi-layers-half" },
            new Category { Name = "Xi măng & Bê tông", Icon = "bi-patch-check" },
            new Category { Name = "Sắt & Thép", Icon = "bi-reception-4" },
            new Category { Name = "Thiết bị Vệ sinh & Bồn nước", Icon = "bi-house-heart" },
            new Category { Name = "Ống nước & Phụ kiện", Icon = "bi-pip-fill" },
            new Category { Name = "Thiết bị Điện & Chiếu sáng", Icon = "bi-lightning-charge" },
            new Category { Name = "Sơn & Chống thấm", Icon = "bi-paint-bucket" },
            new Category { Name = "Keo dán gạch & Chà ron", Icon = "bi-magic" },
            new Category { Name = "Dụng cụ kim khí & Đồ nghề", Icon = "bi-tools" },
            new Category { Name = "Khác", Icon = "bi-box" }
        };
        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();

        // 2. Trigger bulk import from Excel-sourced data
        await AddImportedMaterialsAsync(context, categories);
        await context.SaveChangesAsync();
    }

    private async Task AddImportedMaterialsAsync(KhoDbContext context, List<Category> categories)
    {
        // Define Suppliers
        var supplierNames = new List<string>
        {
            "Anh Lành", "Anh Lộc", "DPL", "Dinh ghe cát", "Dân",
            "Ghe Hải", "Ghe cường", "Ghe cường lò Thanh Bình", "Grand", "Hiền Thanh Phú",
            "Hiền thanh phú", "Hùng", "Kiện Chín Phước", "Lành Ý Mỹ", "Mỹ Hòa",
            "NHT", "Nam Hà Thành", "Nghĩa", "Nguyễn Tình", "Nhà Ý",
            "PAK", "THÀNH PHÁT", "Thanh Phong VTC", "Thuận Phát", "Thành Phát",
            "Thành Trung", "Thái Hoàng", "Thái Trung", "Thắng Dola", "Tocera",
            "Tol Đức Thịnh", "TÀI", "TÂY ĐÔ", "Tài gạch sale", "Tấn Nhã",
            "Tấn Phong", "TỔNG PHÍ", "XUÂN", "Xuân", "Xuân - TOCERA",
            "Xuân - nhà Ý", "Xuân Nhã Ý", "Xuân PAK", "catalan", "nam hà thành",
            "thuận phát", "Đại Phú Lộc", "Đại phú lộc", "Đồng Tâm", "Đức Lộc",
        };

        var suppliers = new Dictionary<string, Supplier>();
        foreach (var name in supplierNames)
        {
            var s = new Supplier { Name = name };
            context.Suppliers.Add(s);
            suppliers[name.ToLower()] = s;
        }
        await context.SaveChangesAsync();

        // Define Materials
        var materialData = new[]
        {
            new { Name = "Cát 1.8 rớt tàu Dũng", Unit = "m", Price = 265000.0m, Category = "Cát", Supplier = "Xuân" },
            new { Name = "cát vàng", Unit = "m", Price = 200000.0m, Category = "Cát", Supplier = "Xuân" },
            new { Name = "2032 hpd", Unit = "m", Price = 0m, Category = "Gạch men 20x40", Supplier = (string)null },
            new { Name = "2111 hpd", Unit = "m", Price = 0m, Category = "Gạch men 20x40", Supplier = (string)null },
            new { Name = "24128T KP", Unit = "m", Price = 0m, Category = "Gạch men 20x40", Supplier = (string)null },
            new { Name = "2484 kp", Unit = "m", Price = 0m, Category = "Gạch men 20x40", Supplier = (string)null },
            new { Name = "25X40 2326 TOCERA thân", Unit = "m", Price = 72000.0m, Category = "Gạch men 25x40", Supplier = "Xuân" },
            new { Name = "25X40 2326 TOCERA viền", Unit = "m", Price = 79000.0m, Category = "Gạch men 25x40", Supplier = "Xuân" },
            new { Name = "Gạch 25x40 Kingming", Unit = "m", Price = 67000.0m, Category = "Gạch men 25x40", Supplier = "Xuân" },
            new { Name = "Nice 2008", Unit = "m", Price = 0m, Category = "Gạch men 25x40", Supplier = (string)null },
            new { Name = "Nice 2008V", Unit = "m", Price = 0m, Category = "Gạch men 25x40", Supplier = (string)null },
            new { Name = "TT 26151", Unit = "m", Price = 0m, Category = "Gạch men 25x40", Supplier = (string)null },
            new { Name = "TT 74483", Unit = "m", Price = 0m, Category = "Gạch men 25x40", Supplier = (string)null },
            new { Name = "TT 74487", Unit = "m", Price = 0m, Category = "Gạch men 25x40", Supplier = (string)null },
            new { Name = "khói ceradoni 2619", Unit = "m", Price = 0m, Category = "Gạch men 25x40", Supplier = (string)null },
            new { Name = "3005 xếp gạch", Unit = "m", Price = 0m, Category = "Gạch men 30x30", Supplier = (string)null },
            new { Name = "30331G", Unit = "m", Price = 0m, Category = "Gạch men 30x30", Supplier = (string)null },
            new { Name = "367 vân xanh", Unit = "m", Price = 0m, Category = "Gạch men 30x30", Supplier = (string)null },
            new { Name = "NY 3020", Unit = "m", Price = 0m, Category = "Gạch men 30x30", Supplier = (string)null },
            new { Name = "NY 330", Unit = "m", Price = 0m, Category = "Gạch men 30x30", Supplier = (string)null },
            new { Name = "Nice 30331 bóng", Unit = "m", Price = 0m, Category = "Gạch men 30x30", Supplier = (string)null },
            new { Name = "Nice 327", Unit = "m", Price = 0m, Category = "Gạch men 30x30", Supplier = (string)null },
            new { Name = "Nice 3332", Unit = "m", Price = 0m, Category = "Gạch men 30x30", Supplier = (string)null },
            new { Name = "Tocera 30769", Unit = "m", Price = 0m, Category = "Gạch men 30x30", Supplier = (string)null },
            new { Name = "VTC 30366", Unit = "m", Price = 0m, Category = "Gạch men 30x30", Supplier = (string)null },
            new { Name = "VTC 30801", Unit = "m", Price = 0m, Category = "Gạch men 30x30", Supplier = (string)null },
            new { Name = "KP 80215", Unit = "m", Price = 0m, Category = "Gạch men 30x45", Supplier = (string)null },
            new { Name = "Tocera 3711", Unit = "m", Price = 0m, Category = "Gạch men 30x45", Supplier = (string)null },
            new { Name = "Tocera 3711 viền", Unit = "m", Price = 0m, Category = "Gạch men 30x45", Supplier = (string)null },
            new { Name = "36055N", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "36055V", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "365 D", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "E35S đậm", Unit = "m", Price = 85000.0m, Category = "Gạch men 30x60", Supplier = "Xuân" },
            new { Name = "HT 3700", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "HT 3702D", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "HT 3702V", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "NY 3640 thân", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "NY 3640BS viền", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "NY 3640D điểm", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "PAK 3279 trắng", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "PAK 360 trắng", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "PAK 3601V3D", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "PAK 3601V3T", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "PAK 3601V3V", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "PAK 3610 Đậm", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "PAK 363D", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "PAK 363T", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "PN 36000 trắng", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "PN 36000D7", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "PN 36000V7", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "PN 3605D", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "PN 3605V", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "PN 39000", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "PN 39000D", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "PN 39000T", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "PN 39000V", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "PN 39001 D", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "PN 39001 T", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "PN 39001 V", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "PN 39300D", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "PN 39300V", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "Thân 36010", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "Tomira 36569", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "Trang trí 36101", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "Viền 36010", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "Viền núi Ý Mỹ", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "Viền tím Ý Mỹ", Unit = "m", Price = 0m, Category = "Gạch men 30x60", Supplier = (string)null },
            new { Name = "40X40 415", Unit = "m", Price = 66000.0m, Category = "Gạch men 40x40", Supplier = "Xuân" },
            new { Name = "Fico 402", Unit = "m", Price = 0m, Category = "Gạch men 40x40", Supplier = (string)null },
            new { Name = "Fico 419 bông xám", Unit = "m", Price = 0m, Category = "Gạch men 40x40", Supplier = (string)null },
            new { Name = "LAN 4204 sân", Unit = "m", Price = 0m, Category = "Gạch men 40x40", Supplier = (string)null },
            new { Name = "NY 4026", Unit = "m", Price = 0m, Category = "Gạch men 40x40", Supplier = (string)null },
            new { Name = "NY 415 sân", Unit = "m", Price = 0m, Category = "Gạch men 40x40", Supplier = (string)null },
            new { Name = "NY 418 sân", Unit = "m", Price = 0m, Category = "Gạch men 40x40", Supplier = (string)null },
            new { Name = "NY 433 cột", Unit = "m", Price = 0m, Category = "Gạch men 40x40", Supplier = (string)null },
            new { Name = "NY 439", Unit = "m", Price = 0m, Category = "Gạch men 40x40", Supplier = (string)null },
            new { Name = "NY 4536 sỏi", Unit = "m", Price = 0m, Category = "Gạch men 40x40", Supplier = (string)null },
            new { Name = "Nice 043 gỗ nâu", Unit = "m", Price = 0m, Category = "Gạch men 40x40", Supplier = (string)null },
            new { Name = "PAK 4170 sân", Unit = "m", Price = 0m, Category = "Gạch men 40x40", Supplier = (string)null },
            new { Name = "PAK 482 mè", Unit = "m", Price = 0m, Category = "Gạch men 40x40", Supplier = (string)null },
            new { Name = "PAK 498 sân", Unit = "m", Price = 0m, Category = "Gạch men 40x40", Supplier = (string)null },
            new { Name = "Rich 44006", Unit = "m", Price = 0m, Category = "Gạch men 40x40", Supplier = (string)null },
            new { Name = "Rich 44009 gỗ nâu", Unit = "m", Price = 0m, Category = "Gạch men 40x40", Supplier = (string)null },
            new { Name = "TTP40x40 4208        TOCERA", Unit = "m", Price = 73000.0m, Category = "Gạch men 40x40", Supplier = "Xuân" },
            new { Name = "Trang trí 434", Unit = "m", Price = 0m, Category = "Gạch men 40x40", Supplier = (string)null },
            new { Name = "Vilacera 1401 nhám", Unit = "m", Price = 0m, Category = "Gạch men 40x40", Supplier = (string)null },
            new { Name = "Vilacera 462 cỏ", Unit = "m", Price = 0m, Category = "Gạch men 40x40", Supplier = (string)null },
            new { Name = "Vilacera 462 nhám", Unit = "m", Price = 0m, Category = "Gạch men 40x40", Supplier = (string)null },
            new { Name = "Đá 40x40 DS4400", Unit = "m", Price = 123000.0m, Category = "Gạch men 40x40", Supplier = "Xuân" },
            new { Name = "HT 4080 trắng", Unit = "m", Price = 0m, Category = "Gạch men 40x80", Supplier = (string)null },
            new { Name = "PAK 481 trắng vân", Unit = "m", Price = 0m, Category = "Gạch men 40x80", Supplier = (string)null },
            new { Name = "PN 483 trắng vân", Unit = "m", Price = 0m, Category = "Gạch men 40x80", Supplier = (string)null },
            new { Name = "HG 55050 trắng vân khói", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "HG 55058 vân xám", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "NY 508", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "NY 5503", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "NY 555", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "NY 569 tím", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "NY 569 xanh", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "NY573", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "NY580", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "Nice 55056 gỗ nâu", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "PAK 501 gỗ nâu", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "PAK 501 vân khói", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "PAK 5015 gỗ", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "PAK 503", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "PAK 5035", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "PAK 514", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "PAK 520 mè", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "PAK 538", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "PAK5022 sân", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "PN E53", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "PN553", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "TTP 517 mè nhạt", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "TTP 521 mè đậm", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "gỗ Restar", Unit = "m", Price = 0m, Category = "Gạch men 50x50", Supplier = (string)null },
            new { Name = "30x60 363 loại 1 thân", Unit = "m", Price = 76388.88888888889m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "30x60 363 loại 1 viền", Unit = "m", Price = 76388.88888888889m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "30x60 363 loại 1 điểm", Unit = "m", Price = 85648.14814814815m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "30x60 K360", Unit = "m", Price = 73000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "30x60 K360V", Unit = "m", Price = 83000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "30x60 Tomira 36569", Unit = "m", Price = 55000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "30x60 nhạt", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "30x60 viền GR55", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "30x60 điểm", Unit = "m", Price = 16020.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "30x60 đậm", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "360 thân", Unit = "m", Price = 81000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "36045", Unit = "m", Price = 84000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "36045D", Unit = "m", Price = 85000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "36045V", Unit = "m", Price = 85000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "60 BK đen", Unit = "m", Price = 141000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "60 Mikado đỏ", Unit = "m", Price = 118000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "60 trắng Mikado", Unit = "m", Price = 100000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "60 đen vân cam", Unit = "m", Price = 132000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "60 đỏ trơn", Unit = "m", Price = 140000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "60 đồng chất Pancera", Unit = "m", Price = 96000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "60X60 ECO69  L1", Unit = "m", Price = 88000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "60x60 6601", Unit = "m", Price = 80000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "60x60 ECO63  L1", Unit = "m", Price = 88000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "60x60 K6602", Unit = "m", Price = 76000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "6601", Unit = "m", Price = 116000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "BK 60 DT 72N", Unit = "m", Price = 109000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "BK 60 xà cừ trắng 2 da", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "BK 60 đen VC khắc kim", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "BK 60 đỏ 2 da", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "BK 60055 vincenza", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "BK 6103 Fico", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "BK 6601 Luxury", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "BK 661 gỗ xám", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "BK 6619 Fico", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "BK 6623", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "BK 66N", Unit = "m", Price = 107000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "BK DL604", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "BK DT67N", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "BK Fico 6907 vân xanh", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "BK trắng 2 da", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "BK trắng Fico", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "BK trắng TP Vietdecor", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "BK trắng Vincenza", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "BK vision xanh", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "BK ĐL604", Unit = "m", Price = 105000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "BK đen TP Mikado", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "BK đen gân tr 6662", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "BK đỏ TP Luxury", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "BK đỏ TP Mikado", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "BK đỏ gân trắng", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "Bk đen VC HT", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "DL604", Unit = "m", Price = 108000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "Gạch 30x60 NY 3640", Unit = "m", Price = 83000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "Gạch 30x60 NY 3640BS", Unit = "m", Price = 85000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "Gạch 30x60 NY 3640D", Unit = "m", Price = 100000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "Gạch 36000 trắng PN", Unit = "m", Price = 81000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "Gạch 60x60 LX60003 vân PN", Unit = "m", Price = 83000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "Gạch K6602", Unit = "m", Price = 74000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "Kho Phương Nam GR36055 Nhạt", Unit = "m", Price = 67000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "Kho Phương Nam GR36055 Đậm", Unit = "m", Price = 67000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "Kho Phương Nam GR36057 Viền", Unit = "m", Price = 67000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "M3606 T", Unit = "m", Price = 75925.92592592593m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "M3606 V", Unit = "m", Price = 75925.92592592593m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "M3606 Đ", Unit = "m", Price = 85185.18518518518m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "MQ 3606 T", Unit = "m", Price = 76388.88888888889m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "MQ 3606 V", Unit = "m", Price = 76388.88888888889m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "MQ3606 D", Unit = "m", Price = 85648.14814814815m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "Men 069", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "Men 30x60 DL 3615 Nhạt (170T)", Unit = "m", Price = 82000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "Men 30x60 DL 3615 Điểm (18T)", Unit = "m", Price = 22000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "Men 30x60 DL 3615 Đậm (20T)", Unit = "m", Price = 84000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "Men 30x60 MQ 3610", Unit = "m", Price = 75000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "Men 30x60 P3601", Unit = "m", Price = 73000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "Men 30x60 P3601D", Unit = "m", Price = 83000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "Men 30x60 P3601V3", Unit = "m", Price = 73000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "Men 40x80 460TD đậm", Unit = "m", Price = 93000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "Men 60 PAK", Unit = "m", Price = 76000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "Men 6003", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "Men 607 mè", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "Men 609", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "Men 609 LB", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "Men 6278", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "Men 6601", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "Men 6602", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "Men KP 660056", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "NaT", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "PAK K6604", Unit = "m", Price = 76000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "PAK MQ3606", Unit = "m", Price = 75000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "PAK giả đá 6273", Unit = "m", Price = 0m, Category = "Gạch men 60x60", Supplier = (string)null },
            new { Name = "PN GHC 3605D", Unit = "m", Price = 82000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "PN GHC 3605V", Unit = "m", Price = 82000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "Phương Nam 36000", Unit = "m", Price = 81000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "Phương Nam 36000D7", Unit = "m", Price = 83000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "Phương Nam 36000V7", Unit = "m", Price = 83000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "Vincenza VC60055", Unit = "m", Price = 105000.0m, Category = "Gạch men 60x60", Supplier = "Xuân" },
            new { Name = "40x80 492T", Unit = "m", Price = 90000.0m, Category = "Gạch men 80x80", Supplier = "Xuân" },
            new { Name = "40x80 492V", Unit = "m", Price = 90000.0m, Category = "Gạch men 80x80", Supplier = "Xuân" },
            new { Name = "50X50 580    (THÙNG 5V)", Unit = "m", Price = 73000.0m, Category = "Gạch men 80x80", Supplier = "Xuân" },
            new { Name = "50x50 SM 580", Unit = "m", Price = 74000.0m, Category = "Gạch men 80x80", Supplier = "Xuân" },
            new { Name = "80", Unit = "m", Price = 310000.0m, Category = "Gạch men 80x80", Supplier = "Xuân" },
            new { Name = "BK 80 AMP 88005 L1", Unit = "m", Price = 217000.0m, Category = "Gạch men 80x80", Supplier = "Xuân" },
            new { Name = "BK xà cừ trắng", Unit = "m", Price = 0m, Category = "Gạch men 80x80", Supplier = (string)null },
            new { Name = "DT86N", Unit = "m", Price = 124000.0m, Category = "Gạch men 80x80", Supplier = "Xuân" },
            new { Name = "Men 40x80 4080", Unit = "m", Price = 91000.0m, Category = "Gạch men 80x80", Supplier = "Xuân" },
            new { Name = "Men 40x80 462 Điểm", Unit = "m", Price = 35000.0m, Category = "Gạch men 80x80", Supplier = "Xuân" },
            new { Name = "Men mờ 3800TD Đậm HT", Unit = "m", Price = 16460.0m, Category = "Gạch men 80x80", Supplier = "Xuân" },
            new { Name = "Men mờ 3800TN nhạt HT", Unit = "m", Price = 83000.0m, Category = "Gạch men 80x80", Supplier = "Xuân" },
            new { Name = "Men mờ 3802 HT", Unit = "m", Price = 85000.0m, Category = "Gạch men 80x80", Supplier = "Xuân" },
            new { Name = "Nâu Khắc kim", Unit = "m", Price = 0m, Category = "Gạch men 80x80", Supplier = (string)null },
            new { Name = "Tocera V30801", Unit = "m", Price = 88000.0m, Category = "Gạch men 80x80", Supplier = "Xuân" },
            new { Name = "Bó kiện nhỏ", Unit = "viên", Price = 5000.0m, Category = "Gạch xây dựng", Supplier = "Thanh Tùng xém tròn" },
            new { Name = "Da lu lỗ tròn", Unit = "viên", Price = 860.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình" },
            new { Name = "Hiệp Hưng", Unit = "viên", Price = 0.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình" },
            new { Name = "Hiệp Thành", Unit = "viên", Price = 0.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình" },
            new { Name = "Hưng Phát", Unit = "viên", Price = 0.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình" },
            new { Name = "Mi xá", Unit = "viên", Price = 600.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình" },
            new { Name = "Ngọn Lỗ tròn kiện", Unit = "viên", Price = 850.0m, Category = "Gạch xây dựng", Supplier = "Báo giá Thanh Bình" },
            new { Name = "Ngọn dợt Lỗ tròn kiện", Unit = "viên", Price = 830.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình" },
            new { Name = "Ngọn kiện lỗ tròn", Unit = "viên", Price = 920.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình báo giá" },
            new { Name = "Ngọn kiện lỗ vuông", Unit = "viên", Price = 870.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình báo giá" },
            new { Name = "Ngọn lỗ tròn xá", Unit = "viên", Price = 830.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình" },
            new { Name = "Ngọn lỗ vuông kiện", Unit = "viên", Price = 810.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình" },
            new { Name = "Ngọn đậm + dợt Lỗ tròn kiện", Unit = "viên", Price = 820.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình" },
            new { Name = "Ngọn đậm lỗ tròn kiện", Unit = "viên", Price = 820.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình" },
            new { Name = "Ngọn đậm lỗ vuông kiện", Unit = "viên", Price = 800.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình" },
            new { Name = "Phí VC", Unit = "viên", Price = 80.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình" },
            new { Name = "Phí kiện", Unit = "viên", Price = 80.0m, Category = "Gạch xây dựng", Supplier = "LÒ THANH BÌNH" },
            new { Name = "Phí xá", Unit = "viên", Price = 140.0m, Category = "Gạch xây dựng", Supplier = "LÒ THANH BÌNH" },
            new { Name = "Phước Lộc", Unit = "viên", Price = 0.0m, Category = "Gạch xây dựng", Supplier = "Phí ghe" },
            new { Name = "TC :", Unit = "viên", Price = 0.0m, Category = "Gạch xây dựng", Supplier = "Phí ghe" },
            new { Name = "Thẻ ngọn", Unit = "viên", Price = 800.0m, Category = "Gạch xây dựng", Supplier = "Ghe Tá giao nhà anh Kiểng" },
            new { Name = "Thẻ ngọn kiện", Unit = "viên", Price = 650.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình" },
            new { Name = "Thẻ ngọn kiện 1000", Unit = "viên", Price = 730.0m, Category = "Gạch xây dựng", Supplier = "Thanh Tùng xém tròn" },
            new { Name = "Thẻ ngọn kiện 500", Unit = "viên", Price = 740.0m, Category = "Gạch xây dựng", Supplier = "Thanh Tùng xém tròn" },
            new { Name = "Thẻ xém", Unit = "viên", Price = 750.0m, Category = "Gạch xây dựng", Supplier = "Ghe Tá giao nhà anh Kiểng" },
            new { Name = "Thẻ xém kiện", Unit = "viên", Price = 630.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình" },
            new { Name = "Thẻ xém kiện 1000", Unit = "viên", Price = 710.0m, Category = "Gạch xây dựng", Supplier = "Thanh Tùng xém tròn" },
            new { Name = "Thẻ xém kiện 500", Unit = "viên", Price = 720.0m, Category = "Gạch xây dựng", Supplier = "Thanh Tùng xém tròn" },
            new { Name = "Tổng", Unit = "viên", Price = 0.0m, Category = "Gạch xây dựng", Supplier = "Thanh BÌnh" },
            new { Name = "VC", Unit = "viên", Price = 80.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình" },
            new { Name = "Vận chuyển", Unit = "viên", Price = 80.0m, Category = "Gạch xây dựng", Supplier = "Thanh Tùng" },
            new { Name = "Xém Lỗ vuông kiện", Unit = "viên", Price = 800.0m, Category = "Gạch xây dựng", Supplier = "Báo giá Thanh Bình" },
            new { Name = "Xém kiện", Unit = "viên", Price = 830.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình" },
            new { Name = "Xém kiện lỗ tròn", Unit = "viên", Price = 900.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình báo giá" },
            new { Name = "Xém kiện lỗ vuông", Unit = "viên", Price = 850.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình báo giá" },
            new { Name = "Xém lỗ tròn kiện", Unit = "viên", Price = 900.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình" },
            new { Name = "Xém lỗ tròn xá", Unit = "viên", Price = 820.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình" },
            new { Name = "Xém xá lỗ vuông", Unit = "viên", Price = 750.0m, Category = "Gạch xây dựng", Supplier = "Báo giá Thanh Bình" },
            new { Name = "hdr", Unit = "viên", Price = 0.0m, Category = "Gạch xây dựng", Supplier = "Lò Thanh Bình" },
            new { Name = "ngọn kiện", Unit = "viên", Price = 850.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình" },
            new { Name = "ngọn xá lỗ vuông", Unit = "viên", Price = 760.0m, Category = "Gạch xây dựng", Supplier = "Báo giá Thanh Bình" },
            new { Name = "v", Unit = "viên", Price = 90.0m, Category = "Gạch xây dựng", Supplier = "Ghe Hải" },
            new { Name = "Ánh Nguyệt", Unit = "viên", Price = 0.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình" },
            new { Name = "Đề mi", Unit = "viên", Price = 700.0m, Category = "Gạch xây dựng", Supplier = "Thanh Tùng xém tròn" },
            new { Name = "Đề mi kiện", Unit = "viên", Price = 650.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình" },
            new { Name = "Đề mi xá", Unit = "viên", Price = 600.0m, Category = "Gạch xây dựng", Supplier = "Thanh Bình" },
            new { Name = "Ống dalu kiện", Unit = "viên", Price = 780.0m, Category = "Gạch xây dựng", Supplier = "LÒ THANH BÌNH" },
            new { Name = "Ống dalu xá", Unit = "viên", Price = 760.0m, Category = "Gạch xây dựng", Supplier = "LÒ ÁNH NGUYỆT" },
            new { Name = "Ống ngọn lỗ tròn xá", Unit = "viên", Price = 870.0m, Category = "Gạch xây dựng", Supplier = "Ghe Tá giao nhà anh Kiểng" },
            new { Name = "Ống xém lỗ tròn xá", Unit = "viên", Price = 870.0m, Category = "Gạch xây dựng", Supplier = "Ghe Tá giao nhà anh Kiểng" },
            new { Name = "Ống xém xá", Unit = "viên", Price = 800.0m, Category = "Gạch xây dựng", Supplier = "LÒ ÁNH NGUYỆT" },
            new { Name = "ống ngọn xá", Unit = "viên", Price = 740.0m, Category = "Gạch xây dựng", Supplier = "LÒ THANH BÌNH" },
            new { Name = "ống xém kiện", Unit = "viên", Price = 810.0m, Category = "Gạch xây dựng", Supplier = "LÒ THANH BÌNH" },
            new { Name = "20 tặng 1", Unit = "bao", Price = 0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "2008T", Unit = "bao", Price = 60000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "2008V", Unit = "bao", Price = 60000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "30x30 V30366", Unit = "thùng", Price = 88000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "30x45 PVY3711", Unit = "288", Price = 70000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "30x45 PY3712", Unit = "120", Price = 70000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "363 viền", Unit = "bao", Price = 81000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "3634D", Unit = "m", Price = 21000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "3634T", Unit = "m", Price = 82000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "3634V", Unit = "m", Price = 94000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "40 gỗ nâu 043", Unit = "112", Price = 66000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "40 gỗ xám 444", Unit = "112", Price = 66000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "50x50 SM561", Unit = "80T", Price = 79000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "50x50 SM569", Unit = "80T", Price = 74000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "6 balet", Unit = "bao", Price = 50000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "61 BK trắng", Unit = "m", Price = 121000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "BK 100 X 100 1010005S2", Unit = "Viên", Price = 285000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "BK 67N", Unit = "64", Price = 107000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "BK Fico 6611", Unit = "96", Price = 113000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "BK Fico 6619", Unit = "128", Price = 113000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "BK Fico 6619 (6 kiện)", Unit = "192", Price = 113000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "BK hoa cương 6615 catalan", Unit = "m", Price = 182000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "BK đỏ gân trắng 6616 catalan", Unit = "m", Price = 187000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Bàn cầu V37 CT bán mẫu tặng la + chân", Unit = "bao", Price = 2790000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Bể 5T tính ra + thêm vốn 3.000/m", Unit = "bao", Price = 0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Bớt", Unit = "bao", Price = 0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "CHUYỂN KHOẢN TRẢ", Unit = "bao", Price = 0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Chuyển trả đủ", Unit = "bao", Price = 0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Chân dola", Unit = "cái", Price = 120000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Cầu 2 nhấn dola", Unit = "bộ", Price = 630000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Cầu Dola mini 1 nhấn", Unit = "bộ", Price = 510000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "DPL 39300D", Unit = "30", Price = 87000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "DPL 39300V", Unit = "30", Price = 87000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "DPL483 thân", Unit = "48", Price = 99000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "DT62", Unit = "52", Price = 109000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "DT63N", Unit = "9", Price = 108000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "DT66N", Unit = "240", Price = 109000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "DT83N", Unit = "10", Price = 124000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "ECO63", Unit = "57", Price = 85000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Fico 40 bông xám", Unit = "176", Price = 64000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Fico 40 vân xám", Unit = "176", Price = 64000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Fico TAP6103", Unit = "m", Price = 120000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "GR39000", Unit = "m", Price = 87000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "GR39000D", Unit = "m", Price = 86000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "GR39000V", Unit = "m", Price = 85000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Ghe Hùng 100m", Unit = "bao", Price = 0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Giá 4.000.000/ch", Unit = "bao", Price = 0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Giả hoa cương 517 (6V/T = 1.5m)", Unit = "30", Price = 86000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Giả hoa cương 521 (6V/T = 1.5m)", Unit = "20", Price = 86000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Gạch 3700", Unit = "180", Price = 85000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Gạch 3702D", Unit = "10", Price = 95000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Gạch 3702V", Unit = "20", Price = 85000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Gạch 433 NY trang trí", Unit = "56", Price = 82000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Gạch 50 HG 55050 trắng vân khói 2 kiện", Unit = "144", Price = 75000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Gạch 50 HG 55058 xám 2 kiện", Unit = "144", Price = 75000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Gạch 50 QH5035", Unit = "144T", Price = 73000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Gạch 5071", Unit = "thùng", Price = 86500.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Gạch 5506", Unit = "176T", Price = 75000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Gạch nhà Ý 508", Unit = "160", Price = 70000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Gạch ống", Unit = "bao", Price = 1500.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Gỗ 50 5015", Unit = "thùng", Price = 75000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "HT 2", Unit = "bao", Price = 57000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Hà Thanh  6623", Unit = "160", Price = 107000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Hà Thanh 6624", Unit = "80", Price = 107000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "K361D", Unit = "1", Price = 95000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "K361T", Unit = "1", Price = 85000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "K361V", Unit = "1", Price = 85000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "K362 Thân Nhà máy", Unit = "120", Price = 73000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "K362 Thân kho", Unit = "40", Price = 75000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "K362 Viền kho", Unit = "17", Price = 75000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "K362 Điểm Nhà Máy", Unit = "15", Price = 83000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "K362D", Unit = "21T", Price = 87500.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "K362T", Unit = "42T", Price = 78240.74074074073m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "K362V", Unit = "21T", Price = 78240.74074074073m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "K501", Unit = "215", Price = 72000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Lưới P40", Unit = "kg", Price = 19700.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "M3610 Đậm", Unit = "72T", Price = 75925.92592592593m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "MQ 3612A", Unit = "37T", Price = 78703.7037037037m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Men 40 402 bông xanh", Unit = "m", Price = 68000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Men 4818", Unit = "86", Price = 94000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Men 50 E53", Unit = "128", Price = 68000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "NKSM 569", Unit = "thùng", Price = 78000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Nam Hà Thành", Unit = "364", Price = 108000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Nhà Ý 5503", Unit = "144", Price = 76000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Nhà Ý SV44015", Unit = "80", Price = 68000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Nhà Ý gỗ 44009", Unit = "240", Price = 66000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Nhà ý SV 415", Unit = "240", Price = 68000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "PAK 403 SV", Unit = "240", Price = 64000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "PAK 4129", Unit = "80", Price = 68000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "PAK QH3282", Unit = "135", Price = 75000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "PAK QH3282V", Unit = "38", Price = 75000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "PAK SV4170", Unit = "192", Price = 65000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Phương Nam 39000D", Unit = "20", Price = 84000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Phương Nam 39000T", Unit = "120", Price = 83000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Phương Nam 39000V", Unit = "20", Price = 84000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Phương Nam 39001D", Unit = "30", Price = 84000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Phương Nam 39001T", Unit = "300", Price = 83000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Phương Nam 39001V", Unit = "40", Price = 84000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Phương Nam 39300D", Unit = "30", Price = 87000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Phương Nam 39300V", Unit = "35", Price = 87000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Phương Nam ESV 553", Unit = "320", Price = 79000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "QH3279 D", Unit = "10", Price = 85000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "QH3279 T", Unit = "180", Price = 75000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "QH3279 V", Unit = "13", Price = 77000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "SM569", Unit = "80T", Price = 80000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "SV 558", Unit = "thùng", Price = 78000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "SV K407 PAK", Unit = "42", Price = 73000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "SV4026 giống 577 Nhà Ý", Unit = "160", Price = 68000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "SV415 giống 5071 Nhà Ý", Unit = "160", Price = 66000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "SV4203 giống 4107 nhà ý", Unit = "80", Price = 73000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "SV5506 giống 577 (5V/th)", Unit = "160", Price = 78000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "SV555 Nhà Ý (5V/th)", Unit = "80", Price = 77000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Sáng sóng tròn", Unit = "100", Price = 48000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Sáng sóng vuông", Unit = "100", Price = 60000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Sân 40 415", Unit = "thùng", Price = 66000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Sân 50", Unit = "m", Price = 73000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Sân 50 5503 nhà ý (5V/T = 1.25m ))", Unit = "240", Price = 73000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Sắt 14V", Unit = "cây", Price = 227400.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Sắt phi 10V", Unit = "cây", Price = 104600.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Sắt phi 12V", Unit = "cây", Price = 158680.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Sắt phi 14V", Unit = "cây", Price = 217760.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "TRẢ ĐỦ", Unit = "bao", Price = 0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "TTP30x30 V30366    tocra", Unit = "thùng", Price = 88000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "TTP4208        TOCERA", Unit = "thùng", Price = 73000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Thành Phát", Unit = "kho", Price = 173000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Thái Hoàng", Unit = "giao", Price = 195000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Tiểu nam", Unit = "cái", Price = 120000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Tocera PVY3701", Unit = "thùng", Price = 68500.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Tocera Y3701", Unit = "thùng", Price = 68500.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Tol 2.4m", Unit = "100", Price = 52800.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Tol 3m", Unit = "100", Price = 66000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Tây Đô", Unit = "chành", Price = 187000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "VA3620 nhạt", Unit = "m", Price = 86000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "VA3620 đậm", Unit = "m", Price = 86000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "balet", Unit = "cái", Price = 50000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "bàn", Unit = "1", Price = 4000000.0m, Category = "Khác", Supplier = "TỔNG PHÍ" },
            new { Name = "bộ", Unit = "1", Price = 6730000.0m, Category = "Khác", Supplier = "TỔNG PHÍ" },
            new { Name = "cái", Unit = "1", Price = 1650000.0m, Category = "Khác", Supplier = "TỔNG PHÍ" },
            new { Name = "căn", Unit = "9", Price = 4500000.0m, Category = "Khác", Supplier = "TỔNG PHÍ" },
            new { Name = "ghe Dinh", Unit = "chuyến", Price = 3500000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "gạch 558", Unit = "thùng", Price = 80000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "gạch 569", Unit = "thùng", Price = 80000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "gỗ 40 483", Unit = "thùng", Price = 67000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "insee", Unit = "bao", Price = 74300.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "kg", Unit = "1", Price = 1500000.0m, Category = "Khác", Supplier = "TỔNG PHÍ" },
            new { Name = "lavabo dola", Unit = "cái", Price = 120000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "máy bagac", Unit = "bao", Price = 0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "phí giao hàng", Unit = "bao", Price = 16800.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "th", Unit = "7", Price = 595000.0m, Category = "Khác", Supplier = "TỔNG PHÍ" },
            new { Name = "trừ bể", Unit = "bao", Price = 0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "trừ tiên xe gạch lộn mẫu", Unit = "bao", Price = 0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "xe bagac", Unit = "bao", Price = 0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "ĐT 37000 Thân Hà Thanh", Unit = "240", Price = 82000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "ĐT 3702 Thân Hà Thanh", Unit = "200", Price = 85000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "ĐT 3702 Viền Hà Thanh", Unit = "30", Price = 82000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "ĐT 3702 Điểm", Unit = "20", Price = 87000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "ĐT 62N", Unit = "70", Price = 109000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Đại Phú Lộc", Unit = "120", Price = 92000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Độc quyền 4040", Unit = "thùng", Price = 66000.0m, Category = "Khác", Supplier = "Xuân" },
            new { Name = "Sáng xi măng", Unit = "100", Price = 48000.0m, Category = "Xi măng & Bê tông", Supplier = "Xuân" },
            new { Name = "Xi măng Cần thơ", Unit = "bao", Price = 67000.0m, Category = "Xi măng & Bê tông", Supplier = "Xuân" },
            new { Name = "Xi măng HT2", Unit = "bao", Price = 67500.0m, Category = "Xi măng & Bê tông", Supplier = "Xuân" },
            new { Name = "Giả đá 40 44013", Unit = "thùng", Price = 66000.0m, Category = "Đá, Sỏi", Supplier = "Xuân" },
            new { Name = "Giả đá 40 KS439", Unit = "thùng", Price = 66000.0m, Category = "Đá, Sỏi", Supplier = "Xuân" },
            new { Name = "Giả đá HC 50 517 TTP", Unit = "50", Price = 89330.0m, Category = "Đá, Sỏi", Supplier = "Xuân" },
            new { Name = "Đá 1/2 trắng", Unit = "m", Price = 670000.0m, Category = "Đá, Sỏi", Supplier = "Xuân" },
            new { Name = "Đá 1/2 đen", Unit = "m", Price = 800000.0m, Category = "Đá, Sỏi", Supplier = "Xuân" },
        };

        var categoryMap = categories.ToDictionary(c => c.Name, c => c.Id);
        var materialMap = new Dictionary<string, Material>();

        foreach (var data in materialData)
        {
            // Chuẩn hóa tên (ví dụ: Sắt 10V -> Sắt phi 10V)
            string normalizedName = data.Name.Trim();
            if (normalizedName == "Sắt 10V") normalizedName = "Sắt phi 10V";

            string key = $"{normalizedName.ToLower()}_{data.Unit.ToLower()}";

            if (!materialMap.TryGetValue(key, out var m))
            {
                if (!categoryMap.TryGetValue(data.Category, out int catId))
                {
                    catId = categoryMap.Values.FirstOrDefault();
                }

                m = new Material
                {
                    Name = normalizedName,
                    Unit = data.Unit,
                    CategoryId = catId,
                    StockQty = 0
                };

                context.Materials.Add(m);
                await context.SaveChangesAsync();
                materialMap[key] = m;

                // Tạo lô mặc định cho vật tư mới
                context.MaterialLots.Add(new MaterialLot
                {
                    MaterialId = m.Id,
                    LotNumber = "Mặc định",
                    StockQty = 0,
                    CostPrice = data.Price,
                    BasePrice = data.Price * 1.2m,
                    Note = "Import từ Excel"
                });
            }

            // Link to Supplier (Many-to-Many)
            if (data.Supplier != null && suppliers.TryGetValue(data.Supplier.ToLower(), out var s))
            {
                // Kiểm tra xem đã link chưa để tránh duplicate trong cùng một đợt seed
                bool alreadyLinked = await context.MaterialSuppliers.AnyAsync(ms => ms.MaterialId == m.Id && ms.SupplierId == s.Id);
                if (!alreadyLinked)
                {
                    context.MaterialSuppliers.Add(new MaterialSupplier { MaterialId = m.Id, SupplierId = s.Id });
                }
            }
        }
        await context.SaveChangesAsync();
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        using var context = _dbFactory.CreateDbContext();
        return await context.Categories.ToListAsync();
    }

    public async Task AddCategoryAsync(Category category)
    {
        using var context = _dbFactory.CreateDbContext();
        context.Categories.Add(category);
        await context.SaveChangesAsync();
    }

    public async Task UpdateCategoryAsync(Category category)
    {
        using var context = _dbFactory.CreateDbContext();
        context.Categories.Update(category);
        await context.SaveChangesAsync();
    }

    public async Task DeleteCategoryAsync(int id)
    {
        using var context = _dbFactory.CreateDbContext();
        var category = await context.Categories.FindAsync(id);
        if (category != null)
        {
            context.Categories.Remove(category);
            await context.SaveChangesAsync();
        }
    }

    public async Task UpdateMaterialLotQtyAsync(int lotId, double newQty, string note)
    {
        using var context = _dbFactory.CreateDbContext();
        var lot = await context.MaterialLots.Include(l => l.Material).FirstOrDefaultAsync(l => l.Id == lotId);
        if (lot != null && lot.Material != null)
        {
            var diff = newQty - lot.StockQty;
            if (diff == 0) return;

            lot.StockQty = newQty;
            lot.Material.StockQty += diff; // Cập nhật kho tổng theo độ lệch

            context.InventoryTransactions.Add(new InventoryTransaction
            {
                MaterialId = lot.MaterialId,
                Timestamp = DateTime.Now,
                Type = "Điều chỉnh",
                QtyChange = diff,
                LotNumber = lot.LotNumber,
                Note = $"Điều chỉnh số lượng: {lot.StockQty - diff} -> {newQty}"
            });

            await context.SaveChangesAsync();
        }
    }

    public async Task<List<Material>> GetMasterMaterialsAsync(bool includeDeleted = false)
    {
        using var context = _dbFactory.CreateDbContext();
        var query = context.Materials
            .AsNoTracking();

        if (!includeDeleted)
        {
            query = query.Where(m => !m.IsDeleted);
        }

        return await query
            .Include(m => m.Category)
            .Include(m => m.MaterialSuppliers)
                .ThenInclude(ms => ms.Supplier)
            .Include(m => m.Lots)
            .ToListAsync();
    }

    public async Task<Material?> GetMaterialByIdAsync(int id)
    {
        using var context = _dbFactory.CreateDbContext();
        return await context.Materials
            .Include(m => m.Category)
            .Include(m => m.MaterialSuppliers)
                .ThenInclude(ms => ms.Supplier)
            .Include(m => m.Lots)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task LinkMaterialToSupplierAsync(int materialId, int supplierId)
    {
        using var context = _dbFactory.CreateDbContext();
        var exists = await context.MaterialSuppliers.AnyAsync(ms => ms.MaterialId == materialId && ms.SupplierId == supplierId);
        if (!exists)
        {
            context.MaterialSuppliers.Add(new MaterialSupplier { MaterialId = materialId, SupplierId = supplierId });
            await context.SaveChangesAsync();
        }
    }

    public async Task AddMasterMaterialAsync(Material material, List<int> supplierIds, decimal costPrice, decimal basePrice)
    {
        using var context = _dbFactory.CreateDbContext();

        // Clear internal collections before add to prevent issues if they were populated
        material.MaterialSuppliers = new();

        context.Materials.Add(material);
        await context.SaveChangesAsync();

        // Add Suppliers
        if (supplierIds != null && supplierIds.Any())
        {
            foreach (var sid in supplierIds)
            {
                context.MaterialSuppliers.Add(new MaterialSupplier { MaterialId = material.Id, SupplierId = sid });
            }
        }

        // Tự động tạo lô Mặc định cho sản phẩm mới
        context.MaterialLots.Add(new MaterialLot
        {
            MaterialId = material.Id,
            LotNumber = "Mặc định",
            StockQty = material.StockQty,
            CostPrice = costPrice,
            BasePrice = basePrice,
            Note = "Lô khởi tạo tự động"
        });
        await context.SaveChangesAsync();
    }

    public async Task UpdateMasterMaterialAsync(Material material, List<int> supplierIds)
    {
        using var context = _dbFactory.CreateDbContext();
        var old = await context.Materials.AsNoTracking().Include(m => m.Lots).FirstOrDefaultAsync(m => m.Id == material.Id);

        if (old != null && (old.Lots == null || !old.Lots.Any()))
        {
            if (old.StockQty != material.StockQty)
            {
                var diff = material.StockQty - old.StockQty;
                context.InventoryTransactions.Add(new InventoryTransaction
                {
                    MaterialId = material.Id,
                    Timestamp = DateTime.Now,
                    Type = "Điều chỉnh",
                    QtyChange = diff,
                    Note = $"Điều chỉnh kho tổng: {old.StockQty} -> {material.StockQty}"
                });
            }
        }

        // Update basic info
        context.Materials.Update(material);

        // Update Suppliers
        var existingSuppliers = await context.MaterialSuppliers.Where(ms => ms.MaterialId == material.Id).ToListAsync();
        context.MaterialSuppliers.RemoveRange(existingSuppliers);

        if (supplierIds != null)
        {
            foreach (var sid in supplierIds)
            {
                context.MaterialSuppliers.Add(new MaterialSupplier { MaterialId = material.Id, SupplierId = sid });
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task DeleteMasterMaterialAsync(int id)
    {
        using var context = _dbFactory.CreateDbContext();
        var material = await context.Materials.FindAsync(id);
        if (material != null)
        {
            material.IsDeleted = true;
            context.Materials.Update(material);
            await context.SaveChangesAsync();
        }
    }

    // --- Supplier Methods ---
    public async Task<List<Supplier>> GetSuppliersAsync()
    {
        using var context = _dbFactory.CreateDbContext();
        return await context.Suppliers.ToListAsync();
    }

    public async Task AddSupplierAsync(Supplier supplier)
    {
        using var context = _dbFactory.CreateDbContext();
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();
    }

    public async Task UpdateSupplierAsync(Supplier supplier)
    {
        using var context = _dbFactory.CreateDbContext();
        context.Suppliers.Update(supplier);
        await context.SaveChangesAsync();
    }

    public async Task DeleteSupplierAsync(int id)
    {
        using var context = _dbFactory.CreateDbContext();
        var supplier = await context.Suppliers.FindAsync(id);
        if (supplier != null)
        {
            context.Suppliers.Remove(supplier);
            await context.SaveChangesAsync();
        }
    }

    // --- Customer Methods ---
    public async Task<List<Customer>> GetCustomersAsync()
    {
        using var context = _dbFactory.CreateDbContext();
        return await context.Customers.ToListAsync();
    }

    public async Task AddCustomerAsync(Customer customer)
    {
        using var context = _dbFactory.CreateDbContext();
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
    }

    public async Task UpdateCustomerAsync(Customer customer)
    {
        using var context = _dbFactory.CreateDbContext();
        context.Customers.Update(customer);
        await context.SaveChangesAsync();
    }

    public async Task DeleteCustomerAsync(int id)
    {
        using var context = _dbFactory.CreateDbContext();
        var customer = await context.Customers.FindAsync(id);
        if (customer != null)
        {
            context.Customers.Remove(customer);
            await context.SaveChangesAsync();
        }
    }

    public async Task<(decimal Revenue, decimal Collected, int Deliveries, int ActiveProjects, List<Delivery> RecentDeliveries, List<Payment> RecentPayments)> GetDashboardStatsAsync()
    {
        using var context = _dbFactory.CreateDbContext();

        var deliveries = await context.Deliveries.ToListAsync();
        var revenue = deliveries.Sum(d => d.TotalAmount);
        var deliveryCount = deliveries.Count;
        var activeProjectsCount = await context.Projects.CountAsync(p => !p.IsCompleted);

        var payments = await context.Payments.ToListAsync();
        var collected = payments.Sum(p => p.Amount);

        var recentDeliveries = await context.Deliveries
            .Include(d => d.Project)
            .OrderByDescending(d => d.Timestamp)
            .Take(50) // Take enough for 30-day filter or just a decent amount
            .ToListAsync();

        var recentPayments = await context.Payments
            .Include(p => p.Project)
            .OrderByDescending(p => p.Timestamp)
            .Take(50)
            .ToListAsync();

        return (revenue, collected, deliveryCount, activeProjectsCount, recentDeliveries, recentPayments);
    }

    public async Task<List<Project>> GetProjectsAsync(bool? isCompleted = null)
    {
        using var context = _dbFactory.CreateDbContext();
        var query = context.Projects
            .Include(p => p.Customer)
            .Include(p => p.Materials)
                .ThenInclude(pm => pm.Material)
            .AsQueryable();

        if (isCompleted.HasValue)
        {
            query = query.Where(p => p.IsCompleted == isCompleted.Value);
        }

        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task CompleteProjectAsync(int id)
    {
        using var context = _dbFactory.CreateDbContext();
        var project = await context.Projects.FindAsync(id);
        if (project != null)
        {
            project.IsCompleted = true;
            await context.SaveChangesAsync();
        }
    }

    public async Task<Delivery?> GetDeliveryAsync(int id)
    {
        using var context = _dbFactory.CreateDbContext();
        var delivery = await context.Deliveries
            .Include(d => d.Items)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (delivery != null)
        {
            delivery.Project = await context.Projects
                .Include(p => p.Customer)
                .Include(p => p.Deliveries)
                    .ThenInclude(d => d.Items)
                .Include(p => p.Payments)
                .FirstOrDefaultAsync(p => p.Id == delivery.ProjectId);
        }

        return delivery;
    }

    public async Task<Project?> GetProjectDetailAsync(int id)
    {
        using var context = _dbFactory.CreateDbContext();
        return await context.Projects
            .Include(p => p.Customer)
            .Include(p => p.Materials)
            .Include(p => p.Deliveries)
                .ThenInclude(d => d.Items)
            .Include(p => p.Payments)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Project?> GetProjectByIdAsync(int id) => await GetProjectDetailAsync(id);

    public async Task<List<Delivery>> GetDeliveriesByProjectIdAsync(int projectId)
    {
        using var context = _dbFactory.CreateDbContext();
        return await context.Deliveries
            .Include(d => d.Items)
            .Where(d => d.ProjectId == projectId)
            .OrderByDescending(d => d.Timestamp)
            .ToListAsync();
    }

    public async Task CreateProjectAsync(Project project)
    {
        using var context = _dbFactory.CreateDbContext();
        context.Projects.Add(project);
        await context.SaveChangesAsync();
    }

    public async Task UpdateProjectAsync(Project project)
    {
        using var context = _dbFactory.CreateDbContext();
        context.Projects.Update(project);
        await context.SaveChangesAsync();
    }

    public async Task UpdateProjectMaterialAsync(ProjectMaterial mat)
    {
        using var context = _dbFactory.CreateDbContext();
        context.ProjectMaterials.Update(mat);
        await context.SaveChangesAsync();
    }

    public async Task AddProjectMaterialAsync(ProjectMaterial mat)
    {
        using var context = _dbFactory.CreateDbContext();
        context.ProjectMaterials.Add(mat);
        await context.SaveChangesAsync();
    }

    public async Task<int> RecordDeliveryAsync(Delivery delivery, List<ProjectMaterial> updatedMaterials)
    {
        using var context = _dbFactory.CreateDbContext();

        // Add delivery
        context.Deliveries.Add(delivery);

        // Update project material quantities and deduct from main warehouse stock
        foreach (var mat in updatedMaterials)
        {
            context.ProjectMaterials.Update(mat);

            // Deduct stock from the main catalog based on delivery item quantity
            var deliveredItem = delivery.Items.FirstOrDefault(i => i.ProjectMaterialId == mat.Id);
            if (deliveredItem != null)
            {
                var mainMaterial = await context.Materials.FindAsync(mat.MaterialId);
                if (mainMaterial != null)
                {
                    mainMaterial.StockQty -= deliveredItem.Qty;
                    context.Materials.Update(mainMaterial);

                    // Update Material Lot if specified
                    if (!string.IsNullOrEmpty(deliveredItem.LotNumber))
                    {
                        var lot = await context.MaterialLots.FirstOrDefaultAsync(l => l.MaterialId == mainMaterial.Id && l.LotNumber == deliveredItem.LotNumber);
                        if (lot != null)
                        {
                            lot.StockQty -= deliveredItem.Qty;
                            context.MaterialLots.Update(lot);
                        }
                    }

                    // Record Inventory Transaction
                    context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        MaterialId = mainMaterial.Id,
                        Timestamp = delivery.Timestamp,
                        Type = deliveredItem.Qty < 0 ? "Trả hàng" : "Xuất",
                        QtyChange = -deliveredItem.Qty,
                        ReferenceId = delivery.Id.ToString(),
                        LotNumber = deliveredItem.LotNumber,
                        Note = (deliveredItem.Qty < 0 ? "Khách trả hàng " : "Xuất ") + $"cho dự án: {delivery.Project?.CustomerName ?? "Ẩn danh"}"
                    });
                }
            }
        }

        await context.SaveChangesAsync();
        return delivery.Id;
    }

    // --- Payment Methods ---
    public async Task AddPaymentAsync(Payment payment)
    {
        using var context = _dbFactory.CreateDbContext();
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
    }

    public async Task DeletePaymentAsync(int id)
    {
        using var context = _dbFactory.CreateDbContext();
        var payment = await context.Payments.FindAsync(id);
        if (payment != null)
        {
            context.Payments.Remove(payment);
            await context.SaveChangesAsync();
        }
    }

    // --- Procurement Methods ---
    public async Task<List<PurchaseOrder>> GetPurchaseOrdersAsync()
    {
        using var context = _dbFactory.CreateDbContext();
        return await context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Items)
                .ThenInclude(i => i.Material)
            .OrderByDescending(p => p.Timestamp)
            .ToListAsync();
    }

    public async Task AddPurchaseOrderAsync(PurchaseOrder po)
    {
        using var context = _dbFactory.CreateDbContext();

        // 1. Lưu phiếu nhập hàng chính
        context.PurchaseOrders.Add(po);
        await context.SaveChangesAsync();

        // 2. Tính toán và tách công nợ cho từng nhà cung cấp trong đơn
        var itemGroups = po.Items.GroupBy(i => i.SupplierId ?? po.SupplierId).ToList();

        foreach (var group in itemGroups)
        {
            if (group.Key <= 0) continue;

            var groupTotal = group.Sum(i => (decimal)i.Qty * i.CostPrice);
            var supplier = await context.Suppliers.FindAsync(group.Key);

            context.SupplierPayments.Add(new SupplierPayment
            {
                SupplierId = group.Key,
                Amount = -groupTotal, // Ghi nợ (số âm)
                Timestamp = po.Timestamp,
                Method = "Công nợ",
                Note = $"Nợ từ phiếu nhập #{po.Id:D5}. NCC: {supplier?.Name}"
            });
        }

        // 3. Cập nhật tồn kho và giá cho từng món
        foreach (var item in po.Items)
        {
            var material = await context.Materials.FindAsync(item.MaterialId);
            if (material == null) continue;

            var lotNum = item.LotNumber;
            if (string.IsNullOrWhiteSpace(lotNum)) lotNum = "Mặc định";

            // Tìm lô theo MaterialId, LotNumber
            var lot = await context.MaterialLots.FirstOrDefaultAsync(l =>
                l.MaterialId == material.Id &&
                l.LotNumber == lotNum);

            if (lot == null)
            {
                lot = new MaterialLot
                {
                    MaterialId = material.Id,
                    LotNumber = lotNum,
                    StockQty = item.Qty,
                    CostPrice = item.CostPrice,
                    BasePrice = item.BasePrice ?? 0,
                    Note = lotNum == "Mặc định" ? "Nhập kho chung" : "Nhập theo lô"
                };
                context.MaterialLots.Add(lot);
            }
            else
            {
                lot.StockQty += item.Qty;
                lot.CostPrice = item.CostPrice;
                if (item.BasePrice.HasValue) lot.BasePrice = item.BasePrice.Value;
                context.MaterialLots.Update(lot);
            }

            material.StockQty += item.Qty;
            context.Materials.Update(material);

            var itemSupplierId = item.SupplierId ?? po.SupplierId;
            context.InventoryTransactions.Add(new InventoryTransaction
            {
                MaterialId = material.Id,
                Timestamp = po.Timestamp,
                Type = "Nhập",
                QtyChange = item.Qty,
                LotNumber = lotNum,
                Note = $"Nhập phiếu #{po.Id:D5} (Nguồn: NCC ID {itemSupplierId})",
                ReferenceId = po.Id.ToString()
            });
        }
        await context.SaveChangesAsync();
    }

    // --- Supplier Debt Methods ---
    public async Task<List<SupplierPayment>> GetSupplierPaymentsAsync()
    {
        using var context = _dbFactory.CreateDbContext();
        return await context.SupplierPayments
            .Include(p => p.Supplier)
            .OrderByDescending(p => p.Timestamp)
            .ToListAsync();
    }

    public async Task AddSupplierPaymentAsync(SupplierPayment payment)
    {
        using var context = _dbFactory.CreateDbContext();
        context.SupplierPayments.Add(payment);
        await context.SaveChangesAsync();
    }

    // --- Advanced Inventory Methods ---
    public async Task RecordInventoryTransactionAsync(InventoryTransaction transaction)
    {
        using var context = _dbFactory.CreateDbContext();
        context.InventoryTransactions.Add(transaction);
        await context.SaveChangesAsync();
    }

    public async Task<List<InventoryTransaction>> GetInventoryTransactionsAsync()
    {
        using var context = _dbFactory.CreateDbContext();
        return await context.InventoryTransactions
            .Include(t => t.Material)
            .OrderByDescending(t => t.Timestamp)
            .ToListAsync();
    }

    public async Task<List<Material>> GetLowStockMaterialsAsync()
    {
        using var context = _dbFactory.CreateDbContext();
        return await context.Materials
            .Where(m => !m.IsDeleted && m.StockQty <= m.MinStockLevel)
            .Include(m => m.MaterialSuppliers)
                .ThenInclude(ms => ms.Supplier)
            .Include(m => m.Lots)
            .ToListAsync();
    }

    // --- Customer Return Methods ---
    public async Task<int> AddCustomerReturnAsync(CustomerReturn cr)
    {
        using var context = _dbFactory.CreateDbContext();

        // 1. Save the return record first
        context.CustomerReturns.Add(cr);
        await context.SaveChangesAsync();

        // 2. Prepare itemized delivery records
        var deliveryItems = new List<DeliveryItem>();
        foreach (var item in cr.Items)
        {
            var pm = await context.ProjectMaterials.FindAsync(item.ProjectMaterialId);
            if (pm != null)
            {
                deliveryItems.Add(new DeliveryItem
                {
                    ProjectMaterialId = item.ProjectMaterialId,
                    Name = pm.Name,
                    Unit = pm.Unit,
                    Qty = -item.Qty,
                    Price = item.Price,
                    Subtotal = -item.Subtotal,
                    LotNumber = item.LotNumber
                });

                // Update main warehouse stock
                var mainMat = await context.Materials.FindAsync(pm.MaterialId);
                if (mainMat != null)
                {
                    mainMat.StockQty += item.Qty;
                    var lotNum = string.IsNullOrEmpty(item.LotNumber) ? "Mặc định" : item.LotNumber;
                    var lot = await context.MaterialLots.FirstOrDefaultAsync(l => l.MaterialId == mainMat.Id && l.LotNumber == lotNum);
                    if (lot != null)
                    {
                        lot.StockQty += item.Qty;
                        context.MaterialLots.Update(lot);
                    }

                    context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        MaterialId = mainMat.Id,
                        Timestamp = DateTime.Now,
                        Type = "Trả hàng",
                        QtyChange = item.Qty,
                        LotNumber = lotNum,
                        Note = $"Khách trả hàng. Dự án: {cr.ProjectId}",
                        ReferenceId = "RET-" + cr.Id
                    });
                }
            }
        }

        // 3. Create negative delivery for debt tracking
        var negativeDelivery = new Delivery
        {
            ProjectId = cr.ProjectId,
            Timestamp = cr.Timestamp,
            TotalAmount = -cr.TotalAmount,
            Note = "Khách trả hàng. Phiếu trả số: " + cr.Id + (string.IsNullOrEmpty(cr.Note) ? "" : " - " + cr.Note),
            ItemsTotal = -cr.TotalAmount,
            Items = deliveryItems
        };

        // Fallback only if somehow no items were processed
        if (!negativeDelivery.Items.Any())
        {
            negativeDelivery.Items.Add(new DeliveryItem
            {
                Name = "Trả hàng vật tư (Tổng cộng)",
                Qty = 1,
                Unit = "Lượt",
                Price = -cr.TotalAmount,
                Subtotal = -cr.TotalAmount
            });
        }

        context.Deliveries.Add(negativeDelivery);
        await context.SaveChangesAsync();

        return negativeDelivery.Id;
    }

    public async Task AddMaterialLotAsync(MaterialLot lot)
    {
        using var context = _dbFactory.CreateDbContext();
        context.MaterialLots.Add(lot);
        await context.SaveChangesAsync();

        // Cập nhật kho tổng của vật tư
        var material = await context.Materials.FindAsync(lot.MaterialId);
        if (material != null)
        {
            material.StockQty += lot.StockQty;
            context.Materials.Update(material);

            // Ghi log biến động kho nếu có số lượng khởi tạo
            if (lot.StockQty != 0)
            {
                context.InventoryTransactions.Add(new InventoryTransaction
                {
                    MaterialId = lot.MaterialId,
                    Timestamp = DateTime.Now,
                    Type = "Nhập",
                    QtyChange = lot.StockQty,
                    LotNumber = lot.LotNumber,
                    Note = "Khởi tạo lô mới",
                    ReferenceId = $"LOT-{lot.Id}"
                });
            }
        }
        await context.SaveChangesAsync();
    }

    public async Task UpdateMaterialLotFullAsync(MaterialLot lot)
    {
        using var context = _dbFactory.CreateDbContext();
        context.MaterialLots.Update(lot);

        // Update aggregate stock qty on material
        var material = await context.Materials.Include(m => m.Lots).FirstOrDefaultAsync(m => m.Id == lot.MaterialId);
        if (material != null)
        {
            material.StockQty = material.Lots.Sum(l => l.StockQty);
            context.Materials.Update(material);
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<MaterialLot>> GetMaterialLotsAsync(int materialId)
    {
        using var context = _dbFactory.CreateDbContext();
        return await context.MaterialLots
            .Where(l => l.MaterialId == materialId)
            .OrderBy(l => l.LotNumber)
            .ToListAsync();
    }

    public async Task<List<CustomerReturn>> GetCustomerReturnsByProjectIdAsync(int projectId)
    {
        using var context = _dbFactory.CreateDbContext();
        return await context.CustomerReturns
            .Include(r => r.Items)
                .ThenInclude(i => i.ProjectMaterial)
            .Where(r => r.ProjectId == projectId)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync();
    }

    public async Task<double> GetTotalPromisedQtyAsync(int materialId, string? lotNumber = null)
    {
        using var context = _dbFactory.CreateDbContext();
        var query = context.ProjectMaterials
            .Include(pm => pm.Project)
            .Where(pm => pm.MaterialId == materialId && pm.RemainingQty > 0 && (pm.Project != null && !pm.Project.IsCompleted));

        if (!string.IsNullOrEmpty(lotNumber))
        {
            query = query.Where(m => m.TargetLotNumber == lotNumber);
        }

        return await query.SumAsync(m => m.RemainingQty);
    }

    public async Task<List<InventoryTransaction>> GetInventoryTransactionsByMaterialIdAsync(int materialId)
    {
        using var context = _dbFactory.CreateDbContext();
        return await context.InventoryTransactions
            .Where(t => t.MaterialId == materialId)
            .OrderByDescending(t => t.Timestamp)
            .ToListAsync();
    }

    public async Task<List<ProjectMaterial>> GetActiveProjectCommitmentsAsync(int materialId)
    {
        using var context = _dbFactory.CreateDbContext();
        return await context.ProjectMaterials
            .AsNoTracking()
            .Include(pm => pm.Project)
            .Where(pm => pm.MaterialId == materialId && pm.RemainingQty > 0 && (pm.Project != null && !pm.Project.IsCompleted))
            .ToListAsync();
    }

    // --- RETAIL SALES METHODS ---
    public async Task<int> CreateRetailOrderAsync(RetailOrder order)
    {
        using var context = _dbFactory.CreateDbContext();

        context.RetailOrders.Add(order);
        await context.SaveChangesAsync();

        foreach (var item in order.Items)
        {
            var mainMat = await context.Materials.FindAsync(item.MaterialId);
            if (mainMat != null)
            {
                mainMat.StockQty -= item.Qty;

                var lotNum = string.IsNullOrEmpty(item.LotNumber) ? "Mặc định" : item.LotNumber;
                var lot = await context.MaterialLots.FirstOrDefaultAsync(l => l.MaterialId == mainMat.Id && l.LotNumber == lotNum);
                if (lot != null)
                {
                    lot.StockQty -= item.Qty;
                    context.MaterialLots.Update(lot);
                }

                context.InventoryTransactions.Add(new InventoryTransaction
                {
                    MaterialId = mainMat.Id,
                    Timestamp = order.Timestamp,
                    Type = "Bán lẻ",
                    QtyChange = -item.Qty,
                    LotNumber = lotNum,
                    Note = $"Bán lẻ cho khách: {order.CustomerName} (Đơn số {order.Id})",
                    ReferenceId = "RETAIL-" + order.Id
                });
            }
        }
        await context.SaveChangesAsync();
        return order.Id;
    }

    public async Task<List<RetailOrder>> GetRetailOrdersAsync(DateTime? from, DateTime? to, string? customerName)
    {
        using var context = _dbFactory.CreateDbContext();
        var query = context.RetailOrders
            .Include(r => r.Items)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(r => r.Timestamp >= from.Value);

        if (to.HasValue)
        {
            // Bao gồm đến cuối ngày của 'to'
            var toEndOfDay = to.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(r => r.Timestamp <= toEndOfDay);
        }

        if (!string.IsNullOrWhiteSpace(customerName))
        {
            query = query.Where(r => r.CustomerName.Contains(customerName));
        }

        return await query.OrderByDescending(r => r.Timestamp).ToListAsync();
    }

    public class MaterialDeliveryHistoryDto
    {
        public DateTime Date { get; set; }
        public double Qty { get; set; }
    }

    public async Task<List<MaterialDeliveryHistoryDto>> GetMaterialDeliveryHistoryAsync(int materialId)
    {
        using var context = _dbFactory.CreateDbContext();

        // Lấy từ lịch sử giao hàng của các dự án (Join thủ công di -> d -> pm)
        var projectDeliveries = await context.DeliveryItems
            .Where(di => di.ProjectMaterialId != 0)
            .Join(context.Deliveries,
                di => di.DeliveryId,
                d => d.Id,
                (di, d) => new { di, d })
            .Join(context.ProjectMaterials,
                combined => combined.di.ProjectMaterialId,
                pm => pm.Id,
                (combined, pm) => new { combined.di, combined.d, pm })
            .Where(x => x.pm.MaterialId == materialId)
            .Select(x => new MaterialDeliveryHistoryDto
            {
                Date = x.d.Timestamp,
                Qty = x.di.Qty
            })
            .ToListAsync();

        // Lấy thêm từ lịch sử bán lẻ (RetailOrder)
        var retailDeliveries = await context.RetailOrderItems
            .Where(roi => roi.MaterialId == materialId)
            .Join(context.RetailOrders,
                roi => roi.RetailOrderId,
                ro => ro.Id,
                (roi, ro) => new { roi, ro })
            .Select(x => new MaterialDeliveryHistoryDto
            {
                Date = x.ro.Timestamp,
                Qty = x.roi.Qty
            })
            .ToListAsync();

        return projectDeliveries
            .Concat(retailDeliveries)
            .OrderBy(x => x.Date)
            .ToList();
    }
}
