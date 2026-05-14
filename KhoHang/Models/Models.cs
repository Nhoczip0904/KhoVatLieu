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
    public string? ProductCode { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string Unit { get; set; } = string.Empty;
    public double StockQty { get; set; } = 0;
    public double MinStockLevel { get; set; } = 0;
    public string? ImageUrl { get; set; }
    
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public List<MaterialLot> Lots { get; set; } = new();
}

public class MaterialLot
{
    public int Id { get; set; }
    public int MaterialId { get; set; }
    public Material? Material { get; set; }
    
    [Required]
    public string LotNumber { get; set; } = string.Empty;
    public double StockQty { get; set; }
    public decimal CostPrice { get; set; }
    public decimal BasePrice { get; set; }
    public DateTime? ProductionDate { get; set; }
    public string? Note { get; set; } // For color variations, etc.
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
    public int InvoiceMode { get; set; } = 1; // 0: Chỉ vật tư, 1: Đối soát tài chính (mặc định)
    
    public List<ProjectMaterial> Materials { get; set; } = new();
    public List<Delivery> Deliveries { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
}

public class ProjectMaterial
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int MaterialId { get; set; }
    public Material? Material { get; set; }
    
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

    public string? LotNumber { get; set; } // Which lot was delivered
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
    public bool ShowOnInvoice { get; set; } = true;
}

// --- PROCUREMENT & SUPPLIER DEBT ---
public class PurchaseOrder
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
    
    public List<PurchaseOrderItem> Items { get; set; } = new();
}

public class PurchaseOrderItem
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public int MaterialId { get; set; }
    public Material? Material { get; set; }
    public double Qty { get; set; }
    public decimal CostPrice { get; set; }
    public decimal Subtotal { get; set; }

    public string? LotNumber { get; set; } // Specified during import
    public decimal? BasePrice { get; set; } // Proposed selling price for this lot
}

public class SupplierPayment
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Method { get; set; } = "Tiền mặt";
    public string? Note { get; set; }
}

// --- ADVANCED INVENTORY ---
public class InventoryTransaction
{
    public int Id { get; set; }
    public int MaterialId { get; set; }
    public Material? Material { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Type { get; set; } = string.Empty; // "Nhập", "Xuất", "Điều chỉnh", "Trả hàng"
    public double QtyChange { get; set; }
    public string? ReferenceId { get; set; } // ID of PurchaseOrder, Delivery, etc.
    public string? LotNumber { get; set; }
    public string? Note { get; set; }
}

// --- CUSTOMER RETURNS ---
public class CustomerReturn
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; } // Amount to deduct from Project
    public string? Note { get; set; }
    
    public List<CustomerReturnItem> Items { get; set; } = new();
}

public class CustomerReturnItem
{
    public int Id { get; set; }
    public int CustomerReturnId { get; set; }
    public int ProjectMaterialId { get; set; }
    public ProjectMaterial? ProjectMaterial { get; set; }
    public double Qty { get; set; }
    public decimal Price { get; set; }
    public decimal Subtotal { get; set; }
    public string? LotNumber { get; set; }
}
