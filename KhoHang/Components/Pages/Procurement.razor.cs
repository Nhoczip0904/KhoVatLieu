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
        protected int? selectedMaterialId;
        protected string selectedMaterialName = "";
        protected bool showMaterialModal = false;
        protected int? selectedCategoryId;
        protected List<Category> categories = new();

        protected void OnPoMaterialsSelected(List<Material> selected)
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
                        CostPrice = m.Lots.OrderBy(l => l.Id).FirstOrDefault()?.CostPrice ?? 0,
                        BasePrice = m.Lots.OrderBy(l => l.Id).FirstOrDefault()?.BasePrice ?? 0,
                        LotNumber = "Mặc định"
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

        protected void FilterByCategory(int? categoryId)
        {
            selectedCategoryId = categoryId;
            currentPage = 1;
        }

        protected void ClearMaterialFilter()
        {
            selectedMaterialId = null;
            selectedMaterialName = "";
            currentPage = 1;
        }

        protected void OnFilterMaterialSelected(List<Material> selected)
        {
            if (selected != null && selected.Any())
            {
                var m = selected.First();
                selectedMaterialId = m.Id;
                selectedMaterialName = m.Name;
                showMaterialModal = false;
                currentPage = 1;
            }
        }

        // Phân trang
        protected int currentPage = 1;
        protected int pageSize = 10;
        protected int totalPages => (int)Math.Ceiling((double)AllFilteredPOs.Count() / pageSize);

        protected IEnumerable<PurchaseOrder> AllFilteredPOs => purchaseOrders
            .Where(p => {
                bool matchesSearch = string.IsNullOrWhiteSpace(searchText) || 
                                     p.Id.ToString("D5").Contains(searchText) || 
                                     (p.Supplier?.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                     p.Items.Any(i => materials.FirstOrDefault(m => m.Id == i.MaterialId)?.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false);
                
                bool matchesDate = (!startDate.HasValue || p.Timestamp.Date >= startDate.Value.Date) &&
                                   (!endDate.HasValue || p.Timestamp.Date <= endDate.Value.Date);

                bool matchesSupplier = !selectedSupplierIds.Any() || 
                                       p.Items.Any(i => {
                                           var m = materials.FirstOrDefault(mat => mat.Id == i.MaterialId);
                                           return m != null && selectedSupplierIds.Contains((int)m.SupplierId);
                                       });

                bool matchesCategory = !selectedCategoryId.HasValue ||
                                        p.Items.Any(i => materials.FirstOrDefault(m => m.Id == i.MaterialId)?.CategoryId == selectedCategoryId);

                bool matchesMaterial = !selectedMaterialId.HasValue ||
                                       p.Items.Any(i => i.MaterialId == selectedMaterialId);

                return matchesSearch && matchesDate && matchesSupplier && matchesCategory && matchesMaterial;
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
            categories = await WarehouseSvc.GetCategoriesAsync();
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
            isCreateModalOpen = true;
        }

        protected void CloseCreateModal() => isCreateModalOpen = false;

        protected void AddNewItemRow() => newPo.Items.Add(new PurchaseOrderItem { Qty = 1, LotNumber = "Mặc định" });

        protected void RemoveItemRow(PurchaseOrderItem item)
        {
            newPo.Items.Remove(item);
            CalculateTotal();
        }

        protected void CopyItemRow(PurchaseOrderItem item)
        {
            var index = newPo.Items.IndexOf(item);
            var newItem = new PurchaseOrderItem
            {
                MaterialId = item.MaterialId,
                Material = item.Material,
                Qty = item.Qty,
                CostPrice = item.CostPrice,
                BasePrice = item.BasePrice,
                LotNumber = item.LotNumber
            };

            if (index != -1 && index < newPo.Items.Count - 1)
            {
                newPo.Items.Insert(index + 1, newItem);
            }
            else
            {
                newPo.Items.Add(newItem);
            }
            CalculateTotal();
        }

        protected void OnMaterialChanged(PurchaseOrderItem item, ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var id))
            {
                item.MaterialId = id;
                var m = materials.FirstOrDefault(x => x.Id == id);
                if (m != null) item.CostPrice = m.Lots.OrderBy(l => l.Id).FirstOrDefault()?.CostPrice ?? 0;
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
            sb.Append("<table border='1'>");
            sb.Append($"<tr><td colspan='7' class='title' style='font-size: 1.2rem; font-weight: bold; text-align: center; background-color: #f1f5f9;'>PHIẾU NHẬP HÀNG CHI TIẾT</td></tr>");
            
            var allSupplierNames = po.Items
                .Select(i => materials.FirstOrDefault(m => m.Id == i.MaterialId)?.Supplier?.Name)
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct()
                .ToList();
            if (!allSupplierNames.Any()) allSupplierNames = new List<string?> { po.Supplier?.Name ?? "N/A" };
            var supplierText = string.Join(", ", allSupplierNames);

            sb.Append($"<tr><td colspan='2' class='label' style='font-weight: bold;'>Mã phiếu:</td><td colspan='5'>#{po.Id:D5}</td></tr>");
            sb.Append($"<tr><td colspan='2' class='label' style='font-weight: bold;'>Nhà cung cấp:</td><td colspan='5'>{supplierText}</td></tr>");
            sb.Append($"<tr><td colspan='2' class='label' style='font-weight: bold;'>Ngày nhập:</td><td colspan='5'>{po.Timestamp:dd/MM/yyyy HH:mm}</td></tr>");
            sb.Append($"<tr><td colspan='2' class='label' style='font-weight: bold;'>Ghi chú:</td><td colspan='5'>{po.Note}</td></tr>");
            sb.Append("<tr><td colspan='7' style='height: 10px; border: none;'></td></tr>");

            sb.Append("<tr>");
            string headerStyle = "background-color: #10b981; color: white; font-weight: bold;";
            sb.Append($"<th style='{headerStyle}'>Nhà cung cấp</th>");
            sb.Append($"<th style='{headerStyle}'>Vật tư</th>");
            sb.Append($"<th style='{headerStyle}'>Số Lô</th>");
            sb.Append($"<th style='{headerStyle}'>Số lượng</th>");
            sb.Append($"<th style='{headerStyle}'>Đơn vị</th>");
            sb.Append($"<th style='{headerStyle}'>Đơn giá nhập</th>");
            sb.Append($"<th style='{headerStyle}'>Thành tiền</th>");
            sb.Append("</tr>");

            var groupedItems = po.Items
                .GroupBy(i => materials.FirstOrDefault(m => m.Id == i.MaterialId)?.SupplierId ?? 0)
                .ToList();

            foreach (var group in groupedItems)
            {
                var supplierName = suppliers.FirstOrDefault(s => s.Id == group.Key)?.Name ?? "N/A";
                decimal supplierSubtotal = 0;

                foreach (var item in group)
                {
                    var mat = materials.FirstOrDefault(m => m.Id == item.MaterialId);
                    supplierSubtotal += item.Subtotal;

                    sb.Append("<tr>");
                    sb.Append($"<td>{supplierName}</td>");
                    sb.Append($"<td>{mat?.Name ?? "N/A"}</td>");
                    sb.Append($"<td>{(item.LotNumber == "Mặc định" ? "" : item.LotNumber)}</td>");
                    sb.Append($"<td style='text-align:center;'>{item.Qty:N2}</td>");
                    sb.Append($"<td>{mat?.Unit ?? "N/A"}</td>");
                    sb.Append($"<td style='text-align:right;'>{item.CostPrice:N0}</td>");
                    sb.Append($"<td style='text-align:right;'>{item.Subtotal:N0}</td>");
                    sb.Append("</tr>");
                }
                
                // Subtotal for this supplier within the PO - Corrected to 7 columns total
                sb.Append($"<tr><td colspan='6' style='text-align:right; font-weight:bold; background-color: #f8fafc;'>CỘNG {supplierName.ToUpper()}:</td><td style='text-align:right; font-weight:bold; background-color: #f8fafc;'>{supplierSubtotal:N0}</td></tr>");
            }

            sb.Append($"<tr><td colspan='6' style='text-align:right; font-weight:bold; background-color: #f1f5f9;'>TỔNG CỘNG PHIẾU:</td><td style='text-align:right; font-weight:bold; color: #ef4444; background-color: #f1f5f9;'>{po.TotalAmount:N0} đ</td></tr>");
            sb.Append("</table>");

            await JS.InvokeVoidAsync("downloadExcel", sb.ToString(), $"PhieuNhap_{po.Id:D5}_{DateTime.Now:yyyyMMdd}");
        }

        protected async Task ExportToExcel()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<table border='1'>");
            sb.Append("<tr><td colspan='9' class='title'>BÁO CÁO NHẬP HÀNG THEO NHÀ CUNG CẤP</td></tr>");
            sb.Append($"<tr><td colspan='9' class='label'>Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}</td></tr>");
            sb.Append("<tr><td colspan='9'></td></tr>");

            // Gom tất cả các món hàng từ các phiếu đã lọc
            var allFilteredItems = AllFilteredPOs
                .SelectMany(po => po.Items.Select(i => new { Item = i, PO = po }))
                .ToList();

            // Nhóm theo nhà cung cấp
            var groupedBySupplier = allFilteredItems
                .GroupBy(x => materials.FirstOrDefault(m => m.Id == x.Item.MaterialId)?.SupplierId ?? 0)
                .OrderBy(g => suppliers.FirstOrDefault(s => s.Id == g.Key)?.Name ?? "Z")
                .ToList();

            decimal grandTotal = 0;

            foreach (var group in groupedBySupplier)
            {
                var supplier = suppliers.FirstOrDefault(s => s.Id == group.Key);
                var supplierName = supplier?.Name ?? "N/A";
                decimal supplierTotal = 0;

                sb.Append("<tr><td colspan='9' style='font-weight:bold; font-size:1.1rem; padding: 5px; background-color:#10b981; color:white;'>");
                sb.Append($"NHÀ CUNG CẤP: {supplierName.ToUpper()}");
                sb.Append("</td></tr>");

                sb.Append("<tr>");
                string bulkHeaderStyle = "background-color: #f1f5f9; font-weight: bold;";
                sb.Append($"<th style='{bulkHeaderStyle}'>STT</th>");
                sb.Append($"<th style='{bulkHeaderStyle}'>Mã Phiếu</th>");
                sb.Append($"<th style='{bulkHeaderStyle}'>Ngày Nhập</th>");
                sb.Append($"<th style='{bulkHeaderStyle}'>Vật tư</th>");
                sb.Append($"<th style='{bulkHeaderStyle}'>ĐVT</th>");
                sb.Append($"<th style='{bulkHeaderStyle}'>Số Lô</th>");
                sb.Append($"<th style='{bulkHeaderStyle}'>SL</th>");
                sb.Append($"<th style='{bulkHeaderStyle}'>Đơn giá</th>");
                sb.Append($"<th style='{bulkHeaderStyle}'>Thành tiền</th>");
                sb.Append("</tr>");
                
                int i = 1;
                foreach(var x in group.OrderBy(x => x.PO.Timestamp))
                {
                    var item = x.Item;
                    var po = x.PO;
                    var mat = materials.FirstOrDefault(m => m.Id == item.MaterialId);
                    
                    decimal rowSubtotal = (decimal)item.Qty * item.CostPrice;
                    supplierTotal += rowSubtotal;
                    grandTotal += rowSubtotal;

                    sb.Append("<tr>");
                    sb.Append($"<td class='center'>{i}</td>");
                    sb.Append($"<td class='center'>#{po.Id:D5}</td>");
                    sb.Append($"<td class='center'>{po.Timestamp:dd/MM/yyyy}</td>");
                    sb.Append($"<td>{mat?.Name ?? "N/A"}</td>");
                    sb.Append($"<td class='center'>{mat?.Unit ?? "N/A"}</td>");
                    sb.Append($"<td>{(item.LotNumber == "Mặc định" ? "" : item.LotNumber)}</td>");
                    sb.Append($"<td class='center'>{item.Qty}</td>");
                    sb.Append($"<td class='number'>{item.CostPrice:N0}</td>");
                    sb.Append($"<td class='number'>{rowSubtotal:N0}</td>");
                    sb.Append("</tr>");
                    i++;
                }

                sb.Append($"<tr><td colspan='7' style='border:none; background-color:#f1f5f9;'></td><td class='footer' style='background-color:#f1f5f9; font-weight:bold;'>TỔNG {supplierName.ToUpper()}:</td><td class='footer' style='text-align:right; background-color:#f1f5f9; font-weight:bold;'>{supplierTotal:N0} đ</td></tr>");
                sb.Append("<tr><td colspan='9' style='border:none; height:20px;'></td></tr>");
            }

            sb.Append($"<tr><td colspan='7' style='border:none; background-color:#064e3b;'></td><td class='footer' style='color:white; background-color:#064e3b; font-weight:bold;'>TỔNG CỘNG TẤT CẢ:</td><td class='footer' style='text-align:right; color:white; background-color:#064e3b; font-weight:bold;'>{grandTotal:N0} đ</td></tr>");
            sb.Append("</table>");

            await JS.InvokeVoidAsync("downloadExcel", sb.ToString(), $"BaoCaoNhapHang_TheoNCC_{DateTime.Now:yyyyMMdd}");
        }
    protected void ResetLot(PurchaseOrderItem item)
    {
        item.LotNumber = "";
        item.CostPrice = 0;
        item.BasePrice = 0;
        CalculateTotal();
    }

    protected void SelectLot(PurchaseOrderItem item, MaterialLot lot)
    {
        item.LotNumber = lot.LotNumber;
        item.CostPrice = lot.CostPrice;
        item.BasePrice = lot.BasePrice;
        CalculateTotal();
    }
    }
}
