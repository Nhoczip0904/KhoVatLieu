using KhoHang.Components;
using KhoHang.Data;
using KhoHang.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Cấu hình sẽ được đọc từ launchSettings.json hoặc biến môi trường

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=khohang.db";
builder.Services.AddDbContextFactory<KhoDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<WarehouseService>();
builder.Services.AddScoped<SessionState>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<AiService>();
builder.Services.AddScoped<AiActionBridgeService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

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

    // Bán Lẻ Tables
    var retailSql = @"
        CREATE TABLE IF NOT EXISTS ""RetailOrders"" (
            ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_RetailOrders"" PRIMARY KEY AUTOINCREMENT,
            ""CustomerName"" TEXT NOT NULL,
            ""Phone"" TEXT NULL,
            ""Address"" TEXT NULL,
            ""Timestamp"" TEXT NOT NULL,
            ""TotalAmount"" TEXT NOT NULL,
            ""AmountPaid"" TEXT NOT NULL,
            ""Note"" TEXT NULL
        );
        CREATE TABLE IF NOT EXISTS ""RetailOrderItems"" (
            ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_RetailOrderItems"" PRIMARY KEY AUTOINCREMENT,
            ""RetailOrderId"" INTEGER NOT NULL,
            ""MaterialId"" INTEGER NOT NULL,
            ""Name"" TEXT NOT NULL,
            ""Unit"" TEXT NOT NULL,
            ""Price"" TEXT NOT NULL,
            ""Qty"" REAL NOT NULL,
            ""Subtotal"" TEXT NOT NULL,
            ""LotNumber"" TEXT NULL,
            CONSTRAINT ""FK_RetailOrderItems_RetailOrders_RetailOrderId"" FOREIGN KEY (""RetailOrderId"") REFERENCES ""RetailOrders"" (""Id"") ON DELETE CASCADE,
            CONSTRAINT ""FK_RetailOrderItems_Materials_MaterialId"" FOREIGN KEY (""MaterialId"") REFERENCES ""Materials"" (""Id"") ON DELETE CASCADE
        );
        CREATE INDEX IF NOT EXISTS ""IX_RetailOrderItems_RetailOrderId"" ON ""RetailOrderItems"" (""RetailOrderId"");
        CREATE INDEX IF NOT EXISTS ""IX_RetailOrderItems_MaterialId"" ON ""RetailOrderItems"" (""MaterialId"");
    ";
    await context.Database.ExecuteSqlRawAsync(retailSql);

    // Thêm cột vào các bảng liên quan nếu chưa có
    try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE Materials ADD COLUMN ProductCode TEXT;"); } catch { }
    try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE Materials ADD COLUMN IsDeleted INTEGER NOT NULL DEFAULT 0;"); } catch { }
    try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE MaterialLots ADD COLUMN BasePrice TEXT;"); } catch { }
    try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE PurchaseOrderItems ADD COLUMN BasePrice TEXT;"); } catch { }

    var tables = new[] { "PurchaseOrderItems", "DeliveryItems", "InventoryTransactions", "CustomerReturnItems" };
    foreach (var table in tables)
    {
        try { await context.Database.ExecuteSqlAsync($"ALTER TABLE {table} ADD COLUMN LotNumber TEXT;"); } catch { }
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

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/account/login", async (
    HttpContext httpContext,
    [FromForm] string username,
    [FromForm] string password,
    [FromForm] string? remember) =>
{
    if (username == "ChiHaiKute" && password == "0939532769")
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, username) };
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var isPersistent = remember == "on";
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = isPersistent,
            ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddDays(30) : null
        };

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return Results.Redirect("/dashboard");
    }
    return Results.Redirect("/?error=1");
}).DisableAntiforgery();

app.MapGet("/account/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();