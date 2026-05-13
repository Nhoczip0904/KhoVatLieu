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

        // 1. Categories
        var categories = new List<Category>
        {
            new Category { Name = "Cát các loại", Icon = "bi-moisture" },
            new Category { Name = "Đá các loại", Icon = "bi-gem" },
            new Category { Name = "Gạch xây dựng", Icon = "bi-bricks" },
            new Category { Name = "Xi măng & Bê tông", Icon = "bi-patch-check" },
            new Category { Name = "Sắt & Thép", Icon = "bi-reception-4" }
        };
        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();

        // 2. Suppliers
        var suppliers = new List<Supplier>
        {
            new Supplier { Name = "Kho Tổng Miền Nam", Phone = "0901234567", Address = "Q9, TP.HCM" },
            new Supplier { Name = "Đại lý VLXD Thành Công", Phone = "0988776655", Address = "Dĩ An, Bình Dương" }
        };
        context.Suppliers.AddRange(suppliers);
        await context.SaveChangesAsync();

        // 3. Materials
        var materials = new List<Material>
        {
            new Material { Name = "Cát vàng", Unit = "Khối", CostPrice = 300000, BasePrice = 350000, CategoryId = categories[0].Id, SupplierId = suppliers[0].Id, StockQty = 500 },
            new Material { Name = "Cát xây tô", Unit = "Khối", CostPrice = 180000, BasePrice = 220000, CategoryId = categories[0].Id, SupplierId = suppliers[1].Id, StockQty = 1000 },
            new Material { Name = "Gạch ống 8x18", Unit = "Viên", CostPrice = 900, BasePrice = 1200, CategoryId = categories[2].Id, SupplierId = suppliers[0].Id, StockQty = 15000 },
            new Material { Name = "Xi măng Hà Tiên", Unit = "Bao", CostPrice = 85000, BasePrice = 95000, CategoryId = categories[3].Id, SupplierId = suppliers[1].Id, StockQty = 300 },
            new Material { Name = "Thép Hòa Phát phi 10", Unit = "Cây", CostPrice = 110000, BasePrice = 125000, CategoryId = categories[4].Id, SupplierId = suppliers[0].Id, StockQty = 200 },
            new Material { Name = "Thép cuộn", Unit = "Kg", CostPrice = 15000, BasePrice = 17500, CategoryId = categories[4].Id, SupplierId = suppliers[0].Id, StockQty = 5000 },
            new Material { Name = "Gạch thẻ", Unit = "Viên", CostPrice = 1100, BasePrice = 1400, CategoryId = categories[2].Id, SupplierId = suppliers[0].Id, StockQty = 20000 },
            new Material { Name = "Xi măng trắng", Unit = "Bao", CostPrice = 120000, BasePrice = 135000, CategoryId = categories[3].Id, SupplierId = suppliers[1].Id, StockQty = 150 }
        };
        context.Materials.AddRange(materials);
        await context.SaveChangesAsync();

        // 4. Customers
        var customers = new List<Customer>
        {
            new Customer { Name = "Anh Hoàng - Villa Q2", Phone = "0911223344", Address = "Thảo Điền, Quận 2" },
            new Customer { Name = "Chị Lan - Nhà phố Bình Tân", Phone = "0933445566", Address = "Tên Lửa, Bình Tân" }
        };
        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        // 5. Projects
        var project = new Project
        {
            CustomerId = customers[0].Id,
            CustomerName = customers[0].Name,
            Phone = customers[0].Phone,
            Address = customers[0].Address,
            CreatedAt = DateTime.Now.AddDays(-5),
            Materials = new List<ProjectMaterial>
            {
                new ProjectMaterial { MaterialId = materials[0].Id, Name = materials[0].Name, Unit = materials[0].Unit, CustomPrice = 345000, TotalQty = 100, RemainingQty = 80 },
                new ProjectMaterial { MaterialId = materials[2].Id, Name = materials[2].Name, Unit = materials[2].Unit, CustomPrice = 1150, TotalQty = 10000, RemainingQty = 5000 }
            }
        };
        context.Projects.Add(project);
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
                Note = $"Điều chỉnh Lô {lot.LotNumber}: {lot.StockQty - diff} -> {newQty}"
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

    public async Task AddMasterMaterialAsync(Material material)
    {
        using var context = _dbFactory.CreateDbContext();
        context.Materials.Add(material);
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

    public async Task<(decimal Revenue, int Deliveries, int ActiveProjects, List<Delivery> RecentDeliveries, List<Payment> RecentPayments)> GetDashboardStatsAsync()
    {
        using var context = _dbFactory.CreateDbContext();
        
        var deliveries = await context.Deliveries.ToListAsync();
        var revenue = deliveries.Sum(d => d.TotalAmount);
        var deliveryCount = deliveries.Count;
        var activeProjectsCount = await context.Projects.CountAsync(p => !p.IsCompleted);
        
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

        return (revenue, deliveryCount, activeProjectsCount, recentDeliveries, recentPayments);
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
                        Type = "Xuất",
                        QtyChange = -deliveredItem.Qty,
                        ReferenceId = delivery.Id.ToString(),
                        Note = $"Xuất cho dự án: {delivery.Project?.CustomerName ?? "Ẩn danh"}. Lô: {deliveredItem.LotNumber ?? "N/A"}. Tổng giá trị đơn: {delivery.TotalAmount:N0}đ"
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
                    BasePrice = item.BasePrice,
                    Note = lotNum == "Mặc định" ? "Nhập kho chung" : "Nhập theo lô"
                };
                context.MaterialLots.Add(lot);
            }
            else
            {
                lot.StockQty += item.Qty;
                if (item.BasePrice.HasValue) lot.BasePrice = item.BasePrice;
                context.MaterialLots.Update(lot);
            }

            // Cập nhật giá gốc và tồn kho tổng
            material.CostPrice = item.CostPrice;
            if (item.BasePrice.HasValue && item.BasePrice > 0) material.BasePrice = item.BasePrice.Value;
            material.StockQty += item.Qty;
            
            context.Materials.Update(material);

            context.InventoryTransactions.Add(new InventoryTransaction
            {
                MaterialId = material.Id,
                Timestamp = po.Timestamp,
                Type = "Nhập",
                QtyChange = item.Qty,
                Note = $"Nhập phiếu #{po.Id:D5}. Lô: {lotNum}",
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
            .ToListAsync();
    }

    // --- Customer Return Methods ---
    public async Task AddCustomerReturnAsync(CustomerReturn cr)
    {
        using var context = _dbFactory.CreateDbContext();
        context.CustomerReturns.Add(cr);
        
        foreach (var item in cr.Items)
        {
            // Update project material remaining qty
            var pm = await context.ProjectMaterials.FindAsync(item.ProjectMaterialId);
            if (pm != null)
            {
                // This means the customer returned it, so maybe add it back to main stock
                var mainMaterial = await context.Materials.FindAsync(pm.MaterialId);
                if (mainMaterial != null)
                {
                    mainMaterial.StockQty += item.Qty;
                    context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        MaterialId = mainMaterial.Id,
                        Timestamp = cr.Timestamp,
                        Type = "Trả hàng",
                        QtyChange = item.Qty,
                        Note = $"Khách trả hàng. Dự án: {cr.ProjectId}"
                    });
                }
            }
        }
        
        // Add a negative delivery to reduce project debt
        var negativeDelivery = new Delivery
        {
            ProjectId = cr.ProjectId,
            Timestamp = cr.Timestamp,
            TotalAmount = -cr.TotalAmount,
            Note = "Khách trả hàng. Phiếu trả số: " + cr.Id + " - " + cr.Note,
            ItemsTotal = -cr.TotalAmount,
            Items = new List<DeliveryItem>()
        };
        context.Deliveries.Add(negativeDelivery);
        
        await context.SaveChangesAsync();
    }

    public async Task<List<MaterialLot>> GetMaterialLotsAsync(int materialId)
    {
        using var context = _dbFactory.CreateDbContext();
        return await context.MaterialLots
            .Where(l => l.MaterialId == materialId && l.StockQty != 0)
            .OrderBy(l => l.LotNumber)
            .ToListAsync();
    }
}
