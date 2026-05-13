using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using KhoHang.Models;
using KhoHang.Services;

namespace KhoHang.Components.Pages
{
    public class ProcurementBase : ComponentBase
    {
        [Inject] public WarehouseService WarehouseSvc { get; set; } = default!;
        [Inject] public IJSRuntime JS { get; set; } = default!;

        protected List<PurchaseOrder> purchaseOrders = new();
        protected List<Supplier> suppliers = new();
        protected List<Material> materials = new();
        protected string searchText = "";
        protected bool isCreateModalOpen = false;
        protected bool isViewModalOpen = false;
        protected bool isSelectionModalOpen = false;
        protected PurchaseOrder newPo = new();
        protected PurchaseOrder? selectedPo;

        protected void OnMaterialsSelected(List<Material> selected)
        {
            foreach (var m in selected)
            {
                if (!newPo.Items.Any(i => i.MaterialId == m.Id))
                {
                    newPo.Items.Add(new PurchaseOrderItem 
                    { 
                        MaterialId = m.Id, 
                        Material = m,
                        Qty = 1,
                        CostPrice = m.CostPrice,
                        BasePrice = m.BasePrice
                    });
                }
            }
            CalculateTotal();
        }

        protected List<int> selectedSupplierIds = new();
        protected bool showSupplierDropdown = false;
        protected DateTime? startDate;
        protected DateTime? endDate;

        protected void ToggleSupplierDropdown() => showSupplierDropdown = !showSupplierDropdown;
        
        protected void ToggleSupplier(int id)
        {
            if (selectedSupplierIds.Contains(id)) selectedSupplierIds.Remove(id);
            else selectedSupplierIds.Add(id);
            currentPage = 1; // Reset pagination when filter changes
        }

        protected void ClearSuppliers()
        {
            selectedSupplierIds.Clear();
            currentPage = 1;
        }

        // Phân trang
        protected int currentPage = 1;
        protected int pageSize = 10;
        protected int totalPages => (int)Math.Ceiling((double)AllFilteredPOs.Count() / pageSize);

        protected IEnumerable<PurchaseOrder> AllFilteredPOs => purchaseOrders
            .Where(p => {
                bool matchesSearch = string.IsNullOrWhiteSpace(searchText) || 
                                     p.Id.ToString("D5").Contains(searchText) || 
                                     (p.Supplier?.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false);
                
                bool matchesDate = (!startDate.HasValue || p.Timestamp.Date >= startDate.Value.Date) &&
                                   (!endDate.HasValue || p.Timestamp.Date <= endDate.Value.Date);

                bool matchesSupplier = !selectedSupplierIds.Any() || 
                                       p.Items.Any(i => {
                                           var m = materials.FirstOrDefault(mat => mat.Id == i.MaterialId);
                                           return m != null && selectedSupplierIds.Contains(m.SupplierId);
                                       });

                return matchesSearch && matchesDate && matchesSupplier;
            })
            .OrderByDescending(p => p.Timestamp);

        protected IEnumerable<PurchaseOrder> PagedPOs => AllFilteredPOs
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize);

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
            suppliers = await WarehouseSvc.GetSuppliersAsync();
            materials = await WarehouseSvc.GetMasterMaterialsAsync();
        }

        protected async Task LoadData()
        {
            purchaseOrders = await WarehouseSvc.GetPurchaseOrdersAsync();
            currentPage = 1; // Reset về trang 1 khi load lại
        }

        protected void ChangePage(int page)
        {
            currentPage = page;
        }

        protected void OpenCreateModal()
        {
            newPo = new PurchaseOrder { Timestamp = DateTime.Now, Items = new List<PurchaseOrderItem>() };
            AddNewItemRow();
            isCreateModalOpen = true;
        }

        protected void CloseCreateModal() => isCreateModalOpen = false;

        protected void AddNewItemRow() => newPo.Items.Add(new PurchaseOrderItem { Qty = 1 });

        protected void RemoveItemRow(PurchaseOrderItem item)
        {
            newPo.Items.Remove(item);
            CalculateTotal();
        }

        protected void OnMaterialChanged(PurchaseOrderItem item, ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var id))
            {
                item.MaterialId = id;
                var m = materials.FirstOrDefault(x => x.Id == id);
                if (m != null) item.CostPrice = m.CostPrice;
            }
            CalculateTotal();
        }

        protected void CalculateTotal()
        {
            newPo.TotalAmount = newPo.Items.Sum(i => (decimal)i.Qty * i.CostPrice);
        }

        protected bool isSaving = false;
        protected string validationError = "";

        protected bool CanSave => newPo.Items.Any() && newPo.Items.All(i => i.MaterialId > 0 && i.Qty > 0);

        protected async Task SavePurchaseOrder()
        {
            if (isSaving) return;
            validationError = "";

            try 
            {
                isSaving = true;
                StateHasChanged();

                // Tạo duy nhất 1 phiếu nhập cho tất cả món hàng
                var poForSupplier = new PurchaseOrder
                {
                    // Lấy SupplierId của món đầu tiên làm đại diện (Service sẽ tự bóc tách nợ sau)
                    SupplierId = materials.FirstOrDefault(m => m.Id == newPo.Items.First().MaterialId)?.SupplierId ?? 0,
                    Timestamp = newPo.Timestamp,
                    Note = newPo.Note,
                    Items = newPo.Items.Select(i => new PurchaseOrderItem {
                        MaterialId = i.MaterialId,
                        Qty = i.Qty,
                        CostPrice = i.CostPrice,
                        BasePrice = i.BasePrice,
                        LotNumber = i.LotNumber,
                        Subtotal = (decimal)i.Qty * i.CostPrice
                    }).ToList()
                };
                
                poForSupplier.TotalAmount = poForSupplier.Items.Sum(i => i.Subtotal);
                await WarehouseSvc.AddPurchaseOrderAsync(poForSupplier);

                await LoadData();
                CloseCreateModal();
            }
            catch (Exception ex)
            {
                validationError = "Lỗi hệ thống: " + ex.Message;
            }
            finally
            {
                isSaving = false;
                StateHasChanged();
            }
        }

        protected void ViewDetails(PurchaseOrder po)
        {
            selectedPo = po;
            isViewModalOpen = true;
        }

        protected void CloseViewModal()
        {
            isViewModalOpen = false;
            selectedPo = null;
        }

        protected async Task ExportSingleOrder(PurchaseOrder po)
        {
            if (po == null) return;
            
            var sb = new System.Text.StringBuilder();
            sb.Append("<table>");
            sb.Append($"<tr><td colspan='7' class='title'>PHIẾU NHẬP HÀNG CHI TIẾT</td></tr>");
            // Lấy danh sách tất cả NCC trong đơn
            var allSupplierNames = po.Items
                .Select(i => materials.FirstOrDefault(m => m.Id == i.MaterialId)?.Supplier?.Name)
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct()
                .ToList();
            if (!allSupplierNames.Any()) allSupplierNames = new List<string?> { po.Supplier?.Name ?? "N/A" };
            var supplierText = string.Join(", ", allSupplierNames);

            sb.Append($"<tr><td colspan='2' class='label'>Mã phiếu:</td><td colspan='5'>#{po.Id:D5}</td></tr>");
            sb.Append($"<tr><td colspan='2' class='label'>Nhà cung cấp:</td><td colspan='5'>{supplierText}</td></tr>");
            sb.Append($"<tr><td colspan='2' class='label'>Ngày nhập:</td><td colspan='5'>{po.Timestamp:dd/MM/yyyy HH:mm}</td></tr>");
            sb.Append($"<tr><td colspan='2' class='label'>Ghi chú:</td><td colspan='5'>{po.Note}</td></tr>");
            sb.Append("<tr><td colspan='7'></td></tr>");

            sb.Append("<table border='1'>");
            sb.Append("<tr style='background-color: #10b981; color: white;'>");
            sb.Append("<th>Nhà cung cấp</th><th>Vật tư</th><th>Số Lô</th><th>Số lượng</th><th>Đơn vị</th><th>Đơn giá nhập</th><th>Thành tiền</th><th>Giá bán lẻ đề xuất</th>");
            sb.Append("</tr>");

            foreach (var item in po.Items)
            {
                var mat = materials.FirstOrDefault(m => m.Id == item.MaterialId);
                var supplierName = mat?.Supplier?.Name ?? "N/A";

                sb.Append("<tr>");
                sb.Append($"<td>{supplierName}</td>");
                sb.Append($"<td>{mat?.Name ?? "N/A"}</td>");
                sb.Append($"<td>{item.LotNumber}</td>");
                sb.Append($"<td style='text-align:center;'>{item.Qty:N2}</td>");
                sb.Append($"<td>{item.Material?.Unit}</td>");
                sb.Append($"<td style='text-align:right;'>{item.CostPrice:N0}</td>");
                sb.Append($"<td style='text-align:right;'>{item.Subtotal:N0}</td>");
                sb.Append($"<td style='text-align:right;'>{item.BasePrice?.ToString("N0") ?? "0"}</td>");
                sb.Append("</tr>");
            }

            sb.Append($"<tr><td colspan='5' style='border:none;'></td><td class='footer'>TỔNG CỘNG:</td><td class='footer' style='text-align:right;'>{po.TotalAmount:N0} đ</td></tr>");
            sb.Append("</table>");

            await JS.InvokeVoidAsync("downloadExcel", sb.ToString(), $"PhieuNhap_{po.Id:D5}_{DateTime.Now:yyyyMMdd}");
        }

        protected async Task ExportToExcel()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<table>");
            sb.Append("<tr><td colspan='6' class='title'>BÁO CÁO CHI TIẾT CÁC PHIẾU NHẬP HÀNG</td></tr>");
            sb.Append($"<tr><td colspan='6' class='label'>Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}</td></tr>");
            sb.Append("<tr><td colspan='6'></td></tr>");

            foreach (var po in AllFilteredPOs)
            {
                var allSupplierNames = po.Items
                    .Select(i => materials.FirstOrDefault(m => m.Id == i.MaterialId)?.Supplier?.Name)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()
                    .ToList();
                if (!allSupplierNames.Any()) allSupplierNames = new List<string?> { po.Supplier?.Name ?? "N/A" };
                var supplierText = string.Join(", ", allSupplierNames);

                sb.Append("<tr style='background-color:#f1f5f9;'><td colspan='7' style='font-weight:bold; border-top:2px solid #10b981;'>");
                sb.Append($"PHIEU NHAP: #{po.Id:D5} | NCC: {supplierText} | Ngay: {po.Timestamp:dd/MM/yyyy}");
                sb.Append("</td></tr>");

                sb.Append("<tr class='header'>");
                sb.Append("<th>STT</th><th>Nhà cung cấp</th><th>Vật tư</th><th>Số Lô</th><th>SL</th><th>Đơn giá</th><th>Thành tiền</th>");
                sb.Append("</tr>");
                
                int i = 1;
                foreach(var item in po.Items)
                {
                    var mat = materials.FirstOrDefault(m => m.Id == item.MaterialId);
                    var supplierName = mat?.Supplier?.Name ?? "N/A";

                    sb.Append("<tr>");
                    sb.Append($"<td class='center'>{i}</td>");
                    sb.Append($"<td>{supplierName}</td>");
                    sb.Append($"<td>{mat?.Name ?? "N/A"}</td>");
                    sb.Append($"<td>{item.LotNumber}</td>");
                    sb.Append($"<td class='center'>{item.Qty}</td>");
                    sb.Append($"<td class='number'>{item.CostPrice:N0}</td>");
                    sb.Append($"<td class='number'>{(decimal)item.Qty * item.CostPrice:N0}</td>");
                    sb.Append("</tr>");
                    i++;
                }
                sb.Append($"<tr><td colspan='5' style='border:none;'></td><td class='footer'>TONG PHIEU:</td><td class='footer' style='text-align:right;'>{po.TotalAmount:N0} đ</td></tr>");
                sb.Append("<tr><td colspan='6' style='border:none; height:20px;'></td></tr>");
            }
            sb.Append("</table>");

            await JS.InvokeVoidAsync("downloadExcel", sb.ToString(), $"ChiTietNhapHang_{DateTime.Now:yyyyMMdd}");
        }
    }
}
