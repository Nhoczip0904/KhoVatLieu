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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Materials)
            .WithOne()
            .HasForeignKey(pm => pm.ProjectId);

        modelBuilder.Entity<Project>()
            .HasMany(p => p.Deliveries)
            .WithOne()
            .HasForeignKey(d => d.ProjectId);

        modelBuilder.Entity<Delivery>()
            .HasMany(d => d.Items)
            .WithOne()
            .HasForeignKey(di => di.DeliveryId);

        modelBuilder.Entity<Project>()
            .HasMany(p => p.Payments)
            .WithOne(p => p.Project)
            .HasForeignKey(p => p.ProjectId);
    }
}
