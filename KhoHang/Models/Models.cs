using System.ComponentModel.DataAnnotations;

namespace KhoHang.Models;

public class Category
{
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; } // Bootstrap icon class
}

public class Supplier
{
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }
}

public class Material
{
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string Unit { get; set; } = string.Empty;
    public decimal CostPrice { get; set; }
    public decimal BasePrice { get; set; }
    public double StockQty { get; set; } = 0;
    public string? ImageUrl { get; set; }
    
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
}

public class Customer
{
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }
}

public class Project
{
    public int Id { get; set; }
    
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Required]
    public string CustomerName { get; set; } = string.Empty; // Legacy support or snapshot
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsCompleted { get; set; } = false;
    
    public List<ProjectMaterial> Materials { get; set; } = new();
    public List<Delivery> Deliveries { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
}

public class ProjectMaterial
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int MaterialId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    
    public decimal CustomPrice { get; set; }
    public double TotalQty { get; set; }
    public double RemainingQty { get; set; }
    public string? ImageUrl { get; set; }
}

public class Delivery
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    public decimal OtherFee { get; set; }
    public string? Note { get; set; }
    
    public decimal ItemsTotal { get; set; }
    public decimal PreviousBalance { get; set; }
    public decimal TotalAmount { get; set; } // Current delivery total
    public decimal GrandTotal { get; set; } // Rolling total
    
    public List<DeliveryItem> Items { get; set; } = new();
}

public class DeliveryItem
{
    public int Id { get; set; }
    public int DeliveryId { get; set; }
    public int ProjectMaterialId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public double Qty { get; set; }
    public decimal Subtotal { get; set; }
}

public class Payment
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Method { get; set; } = "Tiền mặt";
    public string? Note { get; set; }
}
