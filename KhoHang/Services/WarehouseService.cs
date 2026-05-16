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
        return await context.Materials
            .Select(m => m.Unit)
            .Distinct()
            .Where(u => !string.IsNullOrEmpty(u))
            .OrderBy(u => u)
            .ToListAsync();
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
        if (await context.Categories.AnyAsync()) return;

        // 1. Categories based on "BẢNG KÊ HÀNG TỒN KHO"
        var categories = new List<Category>
        {
            new Category { Name = "Cát xây dựng", Icon = "bi-moisture" },
            new Category { Name = "Đá các loại", Icon = "bi-gem" },
            new Category { Name = "Gạch xây dựng", Icon = "bi-bricks" },
            new Category { Name = "Ngói lợp & Phụ kiện", Icon = "bi-house" },
            new Category { Name = "Gạch men 20x40", Icon = "bi-grid-3x3" },
            new Category { Name = "Gạch men 25x40", Icon = "bi-grid-3x3" },
            new Category { Name = "Gạch men 30x45", Icon = "bi-grid-3x3" },
            new Category { Name = "Gạch men 30x60", Icon = "bi-grid-3x3" },
            new Category { Name = "Gạch men 40x80", Icon = "bi-grid-3x3" },
            new Category { Name = "Gạch men 30x30", Icon = "bi-grid-3x3" },
            new Category { Name = "Gạch men 40x40", Icon = "bi-grid-3x3" },
            new Category { Name = "Gạch men 50x50", Icon = "bi-grid-3x3" },
            new Category { Name = "Gạch men 60x60", Icon = "bi-grid-3x3" },
            new Category { Name = "Gạch men 80x80", Icon = "bi-grid-3x3" },
            new Category { Name = "Trang trí & Phụ kiện gạch", Icon = "bi-stars" },
            new Category { Name = "Xi măng & Bê tông", Icon = "bi-patch-check" },
            new Category { Name = "Sắt & Thép", Icon = "bi-reception-4" },
            new Category { Name = "Tôn & Xà gồ", Icon = "bi-layers" },
            new Category { Name = "Ống nhựa & Phụ kiện", Icon = "bi-droplet" },
            new Category { Name = "Sơn & Chống thấm", Icon = "bi-paint-bucket" },
            new Category { Name = "Keo & Bột trét", Icon = "bi-patch-plus" },
            new Category { Name = "Gỗ, Lam & Tấm trần", Icon = "bi-border-style" },
            new Category { Name = "Thiết bị Vệ sinh & Bồn nước", Icon = "bi-house-heart" },
            new Category { Name = "Cửa các loại", Icon = "bi-door-open" }
        };
        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();

        // 2. Full Material List from Excel (Tên + Giá nhập + Category)
        var materials = new List<(Material Mat, decimal Cost)>
        {
            (new Material { Name = "Đá 10x20", Unit = "Thùng", CategoryId = categories[1].Id }, 205000m),
            (new Material { Name = "Viền 7x60", Unit = "Thùng", CategoryId = categories[14].Id }, 750000m),
            (new Material { Name = "Viền 10x60", Unit = "Thùng", CategoryId = categories[14].Id }, 1050000m),
            (new Material { Name = "Tranh ốp tường", Unit = "Viên", CategoryId = categories[14].Id }, 125000m),
            (new Material { Name = "Sắt Phi 16 Miền Nam", Unit = "cây", CategoryId = categories[16].Id }, 266500m),
            (new Material { Name = "Sắt Phi 14 Miền Nam", Unit = "cây", CategoryId = categories[16].Id }, 205500m),
            (new Material { Name = "Sắt Phi 12 Miền Nam", Unit = "cây", CategoryId = categories[16].Id }, 150000m),
            (new Material { Name = "Sắt Phi 10 Miền Nam", Unit = "cây", CategoryId = categories[16].Id }, 95000m),
            (new Material { Name = "Sắt Phi 8 Miền Nam", Unit = "kg", CategoryId = categories[16].Id }, 15300m),
            (new Material { Name = "Sắt Phi 6 Miền Nam", Unit = "kg", CategoryId = categories[16].Id }, 15350m),
            (new Material { Name = "Sắt Phi 4 Miền Nam", Unit = "kg", CategoryId = categories[16].Id }, 15800m),
            (new Material { Name = "Kẽm buộc", Unit = "kg", CategoryId = categories[14].Id }, 17500m),
            (new Material { Name = "Lưới P40", Unit = "kg", CategoryId = categories[14].Id }, 17300m),
            (new Material { Name = "Gạch Block 19x19x39", Unit = "Viên", CategoryId = categories[14].Id }, 15500m),
            (new Material { Name = "Gạch ống", Unit = "Viên", CategoryId = categories[2].Id }, 890m),
            (new Material { Name = "Gạch Thẻ", Unit = "Viên", CategoryId = categories[2].Id }, 800m),
            (new Material { Name = "Đá 4/6 Cô Tô", Unit = "Khối", CategoryId = categories[1].Id }, 460000m),
            (new Material { Name = "Đá 1/2 Cô Tô", Unit = "Khối", CategoryId = categories[1].Id }, 560000m),
            (new Material { Name = "Đá 0x4 Cô Tô", Unit = "Khối", CategoryId = categories[1].Id }, 380000m),
            (new Material { Name = "Đá 1/2 Thạnh Phú", Unit = "Khối", CategoryId = categories[1].Id }, 460000m),
            (new Material { Name = "Cát sông nhập khẩu Campuchia", Unit = "Khối", CategoryId = categories[0].Id }, 235000m),
            (new Material { Name = "Trụ đá 3m", Unit = "cây", CategoryId = categories[1].Id }, 105000m),
            (new Material { Name = "Trụ đá 2.5m", Unit = "cây", CategoryId = categories[1].Id }, 82000m),
            (new Material { Name = "Trụ đá 2m", Unit = "cây", CategoryId = categories[1].Id }, 62000m),
            (new Material { Name = "Trụ đá 1.5m", Unit = "cây", CategoryId = categories[1].Id }, 37000m),
            (new Material { Name = "Trụ đá 1.2m", Unit = "cây", CategoryId = categories[1].Id }, 28000m),
            (new Material { Name = "Trụ đá 1m", Unit = "cây", CategoryId = categories[1].Id }, 22000m),
            (new Material { Name = "Xi măng Vicem Hà Tiên  2 PCB40 - Bao 50 kg", Unit = "bao", CategoryId = categories[15].Id }, 63200m),
            (new Material { Name = "Xi măng Insee", Unit = "bao", CategoryId = categories[15].Id }, 76500m),
            (new Material { Name = "Bột trét Ngoại Thất", Unit = "bao", CategoryId = categories[20].Id }, 170000m),
            (new Material { Name = "Keo chà ron", Unit = "kg", CategoryId = categories[20].Id }, 10000m),
            (new Material { Name = "Keo dán Gạch", Unit = "kg", CategoryId = categories[14].Id }, 5000m),
            (new Material { Name = "Lam 3x6", Unit = "cái", CategoryId = categories[21].Id }, 20000m),
            (new Material { Name = "Lam 3x8", Unit = "cái", CategoryId = categories[21].Id }, 24000m),
            (new Material { Name = "Đồng tiền 6T", Unit = "cái", CategoryId = categories[14].Id }, 40000m),
            (new Material { Name = "Đồng Tiền 4T", Unit = "cái", CategoryId = categories[14].Id }, 25000m),
            (new Material { Name = "Đồng Tiền 2.5T", Unit = "cái", CategoryId = categories[14].Id }, 15000m),
            (new Material { Name = "Đầu cột 20", Unit = "cái", CategoryId = categories[14].Id }, 55000m),
            (new Material { Name = "Đầu cột 25", Unit = "cái", CategoryId = categories[14].Id }, 65000m),
            (new Material { Name = "Đầu cột 30", Unit = "cái", CategoryId = categories[14].Id }, 75000m),
            (new Material { Name = "Bệt liền khối S604", Unit = "bộ", CategoryId = categories[14].Id }, 1211760m),
            (new Material { Name = "Bệt BL5", Unit = "bộ", CategoryId = categories[14].Id }, 2401920m),
            (new Material { Name = "Bàn cầu 2 khối", Unit = "bộ", CategoryId = categories[14].Id }, 680000m),
            (new Material { Name = "Bàn cầu giả mỹ", Unit = "cái", CategoryId = categories[14].Id }, 140000m),
            (new Material { Name = "Lavabo", Unit = "cái", CategoryId = categories[22].Id }, 168000m),
            (new Material { Name = "Chân lavabo", Unit = "cái", CategoryId = categories[22].Id }, 160000m),
            (new Material { Name = "Vòi Lavabo", Unit = "cái", CategoryId = categories[22].Id }, 45000m),
            (new Material { Name = "Vòi hồ", Unit = "cái", CategoryId = categories[14].Id }, 240000m),
            (new Material { Name = "Vòi chén", Unit = "cái", CategoryId = categories[14].Id }, 75000m),
            (new Material { Name = "Lược rác", Unit = "cái", CategoryId = categories[14].Id }, 45000m),
            (new Material { Name = "Củ sen", Unit = "cái", CategoryId = categories[14].Id }, 180000m),
            (new Material { Name = "Vòi xịt vệ sinh", Unit = "cái", CategoryId = categories[22].Id }, 45000m),
            (new Material { Name = "Gương", Unit = "cái", CategoryId = categories[14].Id }, 280000m),
            (new Material { Name = "Tủ kệ lavabo", Unit = "cái", CategoryId = categories[22].Id }, 2800000m),
            (new Material { Name = "Kệ kiến", Unit = "cái", CategoryId = categories[14].Id }, 120000m),
            (new Material { Name = "Củ sen nóng lạnh", Unit = "cái", CategoryId = categories[14].Id }, 350000m),
            (new Material { Name = "Sen dây", Unit = "cái", CategoryId = categories[14].Id }, 150000m),
            (new Material { Name = "Val thao", Unit = "cái", CategoryId = categories[14].Id }, 45000m),
            (new Material { Name = "Val nhựa", Unit = "cái", CategoryId = categories[14].Id }, 30000m),
            (new Material { Name = "Phao điện", Unit = "cái", CategoryId = categories[14].Id }, 100000m),
            (new Material { Name = "Sơn Ngoại 1L", Unit = "lon", CategoryId = categories[19].Id }, 280000m),
            (new Material { Name = "Sơn Nội thất 5L", Unit = "lon", CategoryId = categories[19].Id }, 280000m),
            (new Material { Name = "Sơn Nội Thất 18L", Unit = "thùng", CategoryId = categories[19].Id }, 850000m),
            (new Material { Name = "Sơn Ngoại Thất 5L", Unit = "lon", CategoryId = categories[19].Id }, 480000m),
            (new Material { Name = "Sơn Ngoại thất 18L", Unit = "Thùng", CategoryId = categories[19].Id }, 1450000m),
            (new Material { Name = "kệ góc inox", Unit = "cái", CategoryId = categories[14].Id }, 145000m),
            (new Material { Name = "Vòi lò xo", Unit = "cái", CategoryId = categories[14].Id }, 125000m),
            (new Material { Name = "Val T khóa", Unit = "cái", CategoryId = categories[14].Id }, 65000m),
            (new Material { Name = "kệ chén 2 tầng", Unit = "cái", CategoryId = categories[14].Id }, 950000m),
            (new Material { Name = "ngói mũi hài", Unit = "Cái", CategoryId = categories[3].Id }, 4500m),
            (new Material { Name = "gạch sáng", Unit = "Cái", CategoryId = categories[14].Id }, 46000m),
            (new Material { Name = "bánh ú", Unit = "Cái", CategoryId = categories[14].Id }, 11000m),
            (new Material { Name = "Sen Cây", Unit = "bộ", CategoryId = categories[14].Id }, 550000m),
            (new Material { Name = "Kệ inox", Unit = "cái", CategoryId = categories[14].Id }, 280000m),
            (new Material { Name = "Máng inox", Unit = "cái", CategoryId = categories[14].Id }, 50000m),
            (new Material { Name = "Chậu inox 2 học", Unit = "cái", CategoryId = categories[14].Id }, 1050000m),
            (new Material { Name = "Chậu inox 1 học", Unit = "cái", CategoryId = categories[14].Id }, 850000m),
            (new Material { Name = "Bồn Nhựa 300L", Unit = "cái", CategoryId = categories[22].Id }, 853000m),
            (new Material { Name = "Bồn Nhựa 500L", Unit = "cái", CategoryId = categories[22].Id }, 1170000m),
            (new Material { Name = "Bồn nhựa 1000L", Unit = "cái", CategoryId = categories[22].Id }, 1831000m),
            (new Material { Name = "Bồn inox 300L", Unit = "cái", CategoryId = categories[22].Id }, 1803000m),
            (new Material { Name = "Bồn inox 500L", Unit = "cái", CategoryId = categories[22].Id }, 2194000m),
            (new Material { Name = "Khung lafong", Unit = "m", CategoryId = categories[14].Id }, 75000m),
            (new Material { Name = "Mút 3F", Unit = "tấm", CategoryId = categories[14].Id }, 28000m),
            (new Material { Name = "Ống 114", Unit = "cây", CategoryId = categories[18].Id }, 250500m),
            (new Material { Name = "Ống 90", Unit = "cây", CategoryId = categories[18].Id }, 142300m),
            (new Material { Name = "Ống 60", Unit = "cây", CategoryId = categories[18].Id }, 108800m),
            (new Material { Name = "Ống 49", Unit = "cây", CategoryId = categories[18].Id }, 97000m),
            (new Material { Name = "Ống 42", Unit = "cây", CategoryId = categories[18].Id }, 77000m),
            (new Material { Name = "Ống 34", Unit = "cây", CategoryId = categories[18].Id }, 59000m),
            (new Material { Name = "Ống 27", Unit = "cây", CategoryId = categories[18].Id }, 41000m),
            (new Material { Name = "Ống 21", Unit = "cây", CategoryId = categories[18].Id }, 28000m),
            (new Material { Name = "Tol 2.4m", Unit = "tấm", CategoryId = categories[17].Id }, 63000m),
            (new Material { Name = "Tol 2m", Unit = "tấm", CategoryId = categories[17].Id }, 40000m),
            (new Material { Name = "Tol xi măng sóng", Unit = "tấm", CategoryId = categories[17].Id }, 52000m),
            (new Material { Name = "Tol xi măng phẳng", Unit = "tấm", CategoryId = categories[17].Id }, 58000m),
            (new Material { Name = "Cửa nhôm 8x200", Unit = "bộ", CategoryId = categories[23].Id }, 950000m),
            (new Material { Name = "Cửa nhôm 75x190", Unit = "bộ", CategoryId = categories[23].Id }, 850000m),
            (new Material { Name = "Cửa sổ 8x10", Unit = "bộ", CategoryId = categories[23].Id }, 620000m),
            (new Material { Name = "Cửa sổ 10x12", Unit = "bộ", CategoryId = categories[23].Id }, 740000m),
        };

        foreach (var item in materials)
        {
            context.Materials.Add(item.Mat);
            await context.SaveChangesAsync();

            context.MaterialLots.Add(new MaterialLot
            {
                MaterialId = item.Mat.Id,
                LotNumber = "Mặc định",
                StockQty = 0,
                CostPrice = item.Cost,
                BasePrice = item.Cost * 1.2m, // Tự động gợi ý giá bán +20%
                Note = "Sản phẩm mẫu"
            });
        }

        // 3. Extra Tile List from "Sổ làm việc.xlsx"
        var extraMaterials = new List<(Material Mat, decimal Cost)>
        {
            (new Material { Name = "Gạch 20x40 - 24128T KP", Unit = "m", CategoryId = categories[4].Id }, 0m),
            (new Material { Name = "Gạch 20x40 - 2111 hpd", Unit = "m", CategoryId = categories[4].Id }, 0m),
            (new Material { Name = "Gạch 20x40 - 2032 hpd", Unit = "m", CategoryId = categories[4].Id }, 0m),
            (new Material { Name = "Gạch 20x40 - 2484 kp", Unit = "m", CategoryId = categories[4].Id }, 0m),
            (new Material { Name = "Gạch 25x40 - 2530 lộc v", Unit = "m", CategoryId = categories[5].Id }, 0m),
            (new Material { Name = "Gạch 25x40 - 2515 lộc v", Unit = "m", CategoryId = categories[5].Id }, 0m),
            (new Material { Name = "Gạch 25x40 - 2505 lộc v", Unit = "m", CategoryId = categories[5].Id }, 0m),
            (new Material { Name = "Gạch 25x40 - 2525 lộc v", Unit = "m", CategoryId = categories[5].Id }, 0m),
            (new Material { Name = "Gạch 30x45 - 3025 hồng v", Unit = "m", CategoryId = categories[6].Id }, 0m),
            (new Material { Name = "Gạch 30x45 - 3405 hồng v", Unit = "m", CategoryId = categories[6].Id }, 0m),
            (new Material { Name = "Gạch 30x45 - 3045 hồng v", Unit = "m", CategoryId = categories[6].Id }, 0m),
            (new Material { Name = "Gạch 30x60 - 3647 lộc v", Unit = "m", CategoryId = categories[7].Id }, 0m),
            (new Material { Name = "Gạch 30x60 - 3615 lộc v", Unit = "m", CategoryId = categories[7].Id }, 0m),
            (new Material { Name = "Gạch 30x60 - 3605 lộc v", Unit = "m", CategoryId = categories[7].Id }, 0m),
            (new Material { Name = "Gạch 30x60 - 3625 lộc v", Unit = "m", CategoryId = categories[7].Id }, 0m),
            (new Material { Name = "Gạch 40x80 - 4801 Luxury", Unit = "m", CategoryId = categories[8].Id }, 0m),
            (new Material { Name = "Gạch 40x80 - 4802 Luxury", Unit = "m", CategoryId = categories[8].Id }, 0m),
            (new Material { Name = "Gạch 30x30 - 3001 Nice sân", Unit = "m", CategoryId = categories[9].Id }, 0m),
            (new Material { Name = "Gạch 30x30 - 3314 Nice sân", Unit = "m", CategoryId = categories[9].Id }, 0m),
            (new Material { Name = "Gạch 30x30 - 3326 lộc v", Unit = "m", CategoryId = categories[9].Id }, 0m),
            (new Material { Name = "Gạch 30x30 - 3105 lộc v", Unit = "m", CategoryId = categories[9].Id }, 0m),
            (new Material { Name = "Gạch 40x40 - NY 433 cột", Unit = "m", CategoryId = categories[10].Id }, 0m),
            (new Material { Name = "Gạch 40x40 - Rich 44006", Unit = "m", CategoryId = categories[10].Id }, 0m),
            (new Material { Name = "Gạch 40x40 - Vilacera 462 cỏ", Unit = "m", CategoryId = categories[10].Id }, 0m),
            (new Material { Name = "Gạch 40x40 - Trang trí 434", Unit = "m", CategoryId = categories[10].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - TTP 517 mè nhạt", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - TTP 521 mè đậm", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - PAK 5035", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - NY 555", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - NY 569 tím", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - NY 569 xanh", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - PAK 503", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - PAK 5015 gỗ", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - NY573", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - PAK 514", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - PAK 501 gỗ nâu", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - PAK5022 sân", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - Nice 55056 gỗ nâu", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - NY 5503", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - PAK 501 vân khói", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - NY 508", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - NY580", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - PN553", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - PN E53", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - PAK 520 mè", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - PAK 538", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - gỗ Restar", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - HG 55058 vân xám", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 50x50 - HG 55050 trắng vân khói", Unit = "m", CategoryId = categories[11].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - BK DL604", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - BK 66N", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - BK 6623", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - BK 60055 vincenza", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - BK 6619 Fico", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - BK 6601 Luxury", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - BK 6103 Fico", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - BK 661 gỗ xám", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - Men 6602", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - Men 609", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - Men 6278", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - Men 6601", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - Men 609 LB", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - Men 607 mè", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - Men 069", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - PAK giả đá 6273", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - BK vision xanh", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - BK 60 xà cừ trắng 2 da", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - BK 60 đỏ 2 da", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - BK trắng 2 da", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - BK trắng TP Vietdecor", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - BK trắng Vincenza", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - BK trắng Fico", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - BK đen TP Mikado", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - BK đen gân tr 6662", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - BK đỏ TP Mikado", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - BK đỏ TP Luxury", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - BK đỏ gân trắng", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - Men 6003", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - Men KP 660056", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - Bk đen VC HT", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - BK Fico 6907 vân xanh", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - BK 60 đen VC khắc kim", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 60x60 - BK DT67N", Unit = "m", CategoryId = categories[12].Id }, 0m),
            (new Material { Name = "Gạch 80x80 - Nâu Khắc kim", Unit = "m", CategoryId = categories[13].Id }, 0m),
            (new Material { Name = "Gạch 80x80 - BK xà cừ trắng", Unit = "m", CategoryId = categories[13].Id }, 0m),
            (new Material { Name = "Gạch 80x80 - DT 86N", Unit = "m", CategoryId = categories[13].Id }, 0m),
        };

        foreach (var item in extraMaterials)
        {
            context.Materials.Add(item.Mat);
            await context.SaveChangesAsync();

            context.MaterialLots.Add(new MaterialLot
            {
                MaterialId = item.Mat.Id,
                LotNumber = "Mặc định",
                StockQty = 0,
                CostPrice = 0,
                BasePrice = 0,
                Note = "Import từ sổ làm việc"
            });
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

    public async Task<List<Material>> GetMasterMaterialsAsync()
    {
        using var context = _dbFactory.CreateDbContext();
        return await context.Materials
            .AsNoTracking()
            .Include(m => m.Category)
            .Include(m => m.Supplier)
            .Include(m => m.Lots)
            .ToListAsync();
    }

    public async Task<Material?> GetMaterialByIdAsync(int id)
    {
        using var context = _dbFactory.CreateDbContext();
        return await context.Materials
            .Include(m => m.Category)
            .Include(m => m.Supplier)
            .Include(m => m.Lots)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task AddMasterMaterialAsync(Material material, decimal costPrice, decimal basePrice)
    {
        using var context = _dbFactory.CreateDbContext();
        context.Materials.Add(material);
        await context.SaveChangesAsync();

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

    public async Task UpdateMasterMaterialAsync(Material material)
    {
        using var context = _dbFactory.CreateDbContext();
        var old = await context.Materials.AsNoTracking().Include(m => m.Lots).FirstOrDefaultAsync(m => m.Id == material.Id);
        
        // Chỉ ghi log kho tổng nếu vật tư này KHÔNG quản lý theo Lô
        // (Vật tư có lô sẽ được log riêng trong UpdateMaterialLotQtyAsync)
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

        context.Materials.Update(material);
        await context.SaveChangesAsync();
    }

    public async Task DeleteMasterMaterialAsync(int id)
    {
        using var context = _dbFactory.CreateDbContext();
        var material = await context.Materials.FindAsync(id);
        if (material != null)
        {
            context.Materials.Remove(material);
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
                    .ThenInclude(m => m.Supplier)
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
        var itemGroups = po.Items.GroupBy(i => {
            var m = context.Materials.Find(i.MaterialId);
            return m?.SupplierId ?? 0;
        }).ToList();

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

            var lot = await context.MaterialLots.FirstOrDefaultAsync(l => l.MaterialId == material.Id && l.LotNumber == lotNum);
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
                lot.CostPrice = item.CostPrice; // Cập nhật giá nhập mới nhất cho lô này
                if (item.BasePrice.HasValue) lot.BasePrice = item.BasePrice.Value;
                context.MaterialLots.Update(lot);
            }

            material.StockQty += item.Qty;
            
            context.Materials.Update(material);

            context.InventoryTransactions.Add(new InventoryTransaction
            {
                MaterialId = material.Id,
                Timestamp = po.Timestamp,
                Type = "Nhập",
                QtyChange = item.Qty,
                LotNumber = lotNum,
                Note = $"Nhập phiếu #{po.Id:D5}",
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
            .Where(m => m.StockQty <= m.MinStockLevel)
            .Include(m => m.Supplier)
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
}
