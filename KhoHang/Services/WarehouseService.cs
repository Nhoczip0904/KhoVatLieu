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

    public async Task<List<Material>> GetMasterMaterialsAsync()
    {
        using var context = _dbFactory.CreateDbContext();
        return await context.Materials
            .Include(m => m.Category)
            .Include(m => m.Supplier)
            .ToListAsync();
    }

    public async Task<Material?> GetMaterialByIdAsync(int id)
    {
        using var context = _dbFactory.CreateDbContext();
        return await context.Materials
            .Include(m => m.Category)
            .Include(m => m.Supplier)
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

    public async Task<(decimal Revenue, int Deliveries, int ActiveProjects, List<Delivery> RecentDeliveries)> GetDashboardStatsAsync()
    {
        using var context = _dbFactory.CreateDbContext();
        
        var allDeliveries = await context.Deliveries.ToListAsync();
        var revenue = allDeliveries.Sum(d => d.TotalAmount);
        var deliveryCount = allDeliveries.Count;
        var activeProjectsCount = await context.Projects.CountAsync(p => !p.IsCompleted);
        var recent = await context.Deliveries
            .Include(d => d.Project)
            .Include(d => d.Items)
            .OrderByDescending(d => d.Timestamp)
            .Take(5)
            .ToListAsync();

        return (revenue, deliveryCount, activeProjectsCount, recent);
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
}
