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
    context.Database.EnsureCreated();
    
    var service = scope.ServiceProvider.GetRequiredService<WarehouseService>();
    service.SeedDataAsync().Wait();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();