using Microsoft.EntityFrameworkCore;
using KhoHang.Models;

namespace KhoHang.Data;

public class KhoDbContext : DbContext
{
    public KhoDbContext(DbContextOptions<KhoDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMaterial> ProjectMaterials => Set<ProjectMaterial>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<DeliveryItem> DeliveryItems => Set<DeliveryItem>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<CustomerReturn> CustomerReturns => Set<CustomerReturn>();
    public DbSet<CustomerReturnItem> CustomerReturnItems => Set<CustomerReturnItem>();
    public DbSet<MaterialLot> MaterialLots => Set<MaterialLot>();
    public DbSet<MaterialSupplier> MaterialSuppliers => Set<MaterialSupplier>();
    public DbSet<RetailOrder> RetailOrders => Set<RetailOrder>();
    public DbSet<RetailOrderItem> RetailOrderItems => Set<RetailOrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MaterialSupplier>()
            .HasKey(ms => new { ms.MaterialId, ms.SupplierId });

        modelBuilder.Entity<MaterialSupplier>()
            .HasOne(ms => ms.Material)
            .WithMany(m => m.MaterialSuppliers)
            .HasForeignKey(ms => ms.MaterialId);

        modelBuilder.Entity<MaterialSupplier>()
            .HasOne(ms => ms.Supplier)
            .WithMany()
            .HasForeignKey(ms => ms.SupplierId);
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Materials)
            .WithOne(pm => pm.Project)
            .HasForeignKey(pm => pm.ProjectId);

        modelBuilder.Entity<Material>()
            .HasMany(m => m.Lots)
            .WithOne(l => l.Material)
            .HasForeignKey(l => l.MaterialId);

        modelBuilder.Entity<Project>()
            .HasMany(p => p.Deliveries)
            .WithOne(d => d.Project)
            .HasForeignKey(d => d.ProjectId);

        modelBuilder.Entity<Delivery>()
            .HasMany(d => d.Items)
            .WithOne()
            .HasForeignKey(di => di.DeliveryId);

        modelBuilder.Entity<Project>()
            .HasMany(p => p.Payments)
            .WithOne(p => p.Project)
            .HasForeignKey(p => p.ProjectId);

        modelBuilder.Entity<PurchaseOrder>()
            .HasMany(p => p.Items)
            .WithOne()
            .HasForeignKey(pi => pi.PurchaseOrderId);

        modelBuilder.Entity<CustomerReturn>()
            .HasMany(r => r.Items)
            .WithOne()
            .HasForeignKey(ri => ri.CustomerReturnId);

        modelBuilder.Entity<RetailOrder>()
            .HasMany(r => r.Items)
            .WithOne(ri => ri.RetailOrder)
            .HasForeignKey(ri => ri.RetailOrderId);
    }
}
