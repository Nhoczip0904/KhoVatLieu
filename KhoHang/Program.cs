using KhoHang.Components;
using KhoHang.Data;
using KhoHang.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Cấu hình sẽ được đọc từ launchSettings.json hoặc biến môi trường

builder.Services.AddDbContextFactory<KhoDbContext>(options =>
    options.UseSqlite("Data Source=khohang.db"));

builder.Services.AddScoped<WarehouseService>();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<KhoDbContext>>();
    using var context = dbFactory.CreateDbContext();
    await context.Database.EnsureCreatedAsync();

    // Thủ thuật cập nhật schema thủ công cho hệ thống dùng EnsureCreated
    var sql = @"
        CREATE TABLE IF NOT EXISTS ""MaterialLots"" (
            ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_MaterialLots"" PRIMARY KEY AUTOINCREMENT,
            ""MaterialId"" INTEGER NOT NULL,
            ""LotNumber"" TEXT NOT NULL,
            ""StockQty"" REAL NOT NULL,
            ""BasePrice"" TEXT NULL,
            ""ProductionDate"" TEXT NULL,
            ""Note"" TEXT NULL,
            CONSTRAINT ""FK_MaterialLots_Materials_MaterialId"" FOREIGN KEY (""MaterialId"") REFERENCES ""Materials"" (""Id"") ON DELETE CASCADE
        );
        CREATE INDEX IF NOT EXISTS ""IX_MaterialLots_MaterialId"" ON ""MaterialLots"" (""MaterialId"");
    ";
    await context.Database.ExecuteSqlRawAsync(sql);

    // Thêm cột vào các bảng liên quan nếu chưa có
    try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE Materials ADD COLUMN ProductCode TEXT;"); } catch { }
    try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE MaterialLots ADD COLUMN BasePrice TEXT;"); } catch { }
    try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE PurchaseOrderItems ADD COLUMN BasePrice TEXT;"); } catch { }
    
    var tables = new[] { "PurchaseOrderItems", "DeliveryItems", "InventoryTransactions", "CustomerReturnItems" };
    foreach (var table in tables)
    {
        try { await context.Database.ExecuteSqlRawAsync($"ALTER TABLE {table} ADD COLUMN LotNumber TEXT;"); } catch { }
    }
    try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE ProjectMaterials ADD COLUMN TargetLotNumber TEXT;"); } catch { }
    
    var service = scope.ServiceProvider.GetRequiredService<WarehouseService>();
    await service.SeedDataAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Thay cho MapStaticAssets để tương thích ngược
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();