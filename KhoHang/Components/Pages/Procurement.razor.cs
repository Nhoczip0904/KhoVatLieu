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
        private string _searchText = "";
        protected string searchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    currentPage = 1;
                }
            }
        }
        private DateTime? _startDate;
        protected DateTime? startDate
        {
            get => _startDate;
            set
            {
                if (_startDate != value)
                {
                    _startDate = value;
                    currentPage = 1;
                }
            }
        }
        private DateTime? _endDate;
        protected DateTime? endDate
        {
            get => _endDate;
            set
            {
                if (_endDate != value)
                {
                    _endDate = value;
                    currentPage = 1;
                }
            }
        }
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
                        SupplierId = m.MaterialSuppliers?.FirstOrDefault()?.SupplierId, // Mặc định theo NCC đầu tiên trong danh mục
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
            .Where(p =>
            {
                bool matchesSearch = string.IsNullOrWhiteSpace(searchText) ||
                                     p.Id.ToString("D5").Contains(searchText) ||
                                     (p.Supplier?.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                     p.Items.Any(i => materials.FirstOrDefault(m => m.Id == i.MaterialId)?.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false);

                bool matchesDate = (!startDate.HasValue || p.Timestamp.Date >= startDate.Value.Date) &&
                                   (!endDate.HasValue || p.Timestamp.Date <= endDate.Value.Date);

                bool matchesSupplier = !selectedSupplierIds.Any() ||
                                       p.Items.Any(i =>
                                       {
                                           var m = materials.FirstOrDefault(mat => mat.Id == i.MaterialId);
                                           return m != null && m.MaterialSuppliers != null && m.MaterialSuppliers.Any(ms => selectedSupplierIds.Contains(ms.SupplierId));
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
            materials = await WarehouseSvc.GetMasterMaterialsAsync(includeDeleted: true);
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
                SupplierId = item.SupplierId,
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
                    // Lấy SupplierId của món đầu tiên làm đại diện
                    SupplierId = newPo.Items.First().SupplierId ?? 0,
                    Timestamp = newPo.Timestamp,
                    Note = newPo.Note,
                    Items = newPo.Items.Select(i => new PurchaseOrderItem
                    {
                        MaterialId = i.MaterialId,
                        SupplierId = i.SupplierId, // Phải gán NCC cho từng món để tách công nợ
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
                .Select(i => i.SupplierId != null ? suppliers.FirstOrDefault(s => s.Id == i.SupplierId)?.Name : po.Supplier?.Name)
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct()
                .ToList();
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
                .GroupBy(i => i.SupplierId ?? po.SupplierId)
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
            sb.Append("<style>");
            sb.Append("table { border-collapse: collapse; width: 100%; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }");
            sb.Append("th, td { border: 1px solid #e2e8f0; padding: 10px; font-size: 0.9rem; }");
            sb.Append(".title { font-size: 1.5rem; font-weight: 800; text-align: center; color: #1e293b; background-color: #f8fafc; border: none !important; }");
            sb.Append(".label { font-size: 0.85rem; color: #64748b; text-align: center; border: none !important; }");
            sb.Append(".center { text-align: center; }");
            sb.Append(".number { text-align: right; }");
            sb.Append(".footer { font-weight: 800; background-color: #f1f5f9; }");
            sb.Append("</style>");
            sb.Append("<table>");
            sb.Append("<tr><td colspan='9' class='title'>BÁO CÁO CHI TIẾT NHẬP HÀNG</td></tr>");
            sb.Append($"<tr><td colspan='9' class='label'>Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}</td></tr>");
            sb.Append("<tr><td colspan='9' style='border:none; height:15px;'></td></tr>");

            // Gom tất cả các món hàng từ các phiếu đã lọc, và lọc lại từng item theo tiêu chí
            var allFilteredItems = AllFilteredPOs
                .SelectMany(po => po.Items.Select(i => new { Item = i, PO = po }))
                .Where(x =>
                {
                    var item = x.Item;
                    var po = x.PO;
                    var mat = materials.FirstOrDefault(m => m.Id == item.MaterialId);

                    // Lọc theo search text (tên vật tư)
                    bool matchesSearch = string.IsNullOrWhiteSpace(searchText) ||
                                         (mat?.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                         po.Id.ToString("D5").Contains(searchText) ||
                                         (po.Supplier?.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false);

                    // Lọc theo NCC (nếu item có NCC riêng thì dùng, ko thì dùng của PO)
                    var sId = item.SupplierId ?? po.SupplierId;
                    bool matchesSupplier = !selectedSupplierIds.Any() || selectedSupplierIds.Contains(sId);

                    // Lọc theo Nhóm
                    bool matchesCategory = !selectedCategoryId.HasValue || mat?.CategoryId == selectedCategoryId;

                    // Lọc theo Vật tư cụ thể
                    bool matchesMaterial = !selectedMaterialId.HasValue || item.MaterialId == selectedMaterialId;

                    return matchesSearch && matchesSupplier && matchesCategory && matchesMaterial;
                })
                .ToList();

            // Nhóm theo Phiếu nhập (Outer Group)
            var groupedByPO = allFilteredItems
                .GroupBy(x => x.PO.Id)
                .OrderBy(g => g.First().PO.Timestamp)
                .ToList();

            decimal grandTotal = 0;

            foreach (var poGroup in groupedByPO)
            {
                var po = poGroup.First().PO;
                sb.Append("<tr><td colspan='9' style='font-weight:bold; font-size:1.1rem; padding: 10px; background-color:#1e293b; color:white;'>");
                sb.Append($"PHIẾU NHẬP HÀNG #{po.Id:D5} - NGÀY: {po.Timestamp:dd/MM/yyyy HH:mm}");
                if (!string.IsNullOrEmpty(po.Note)) sb.Append($" - Ghi chú: {po.Note}");
                sb.Append("</td></tr>");

                // Nhóm theo Nhà cung cấp bên trong từng phiếu (Inner Group)
                var itemsGroupedBySupplier = poGroup.GroupBy(x => x.Item.SupplierId ?? po.SupplierId).ToList();

                foreach (var sGroup in itemsGroupedBySupplier)
                {
                    var supplierId = sGroup.Key;
                    var supplierName = suppliers.FirstOrDefault(s => s.Id == supplierId)?.Name ?? "N/A";
                    decimal supplierSubtotal = 0;

                    sb.Append("<tr><td colspan='9' style='font-weight:bold; background-color:#f1f5f9; color:#1e293b; padding: 5px;'>");
                    sb.Append($"NCC: {supplierName.ToUpper()}");
                    sb.Append("</td></tr>");

                    sb.Append("<tr>");
                    string bulkHeaderStyle = "background-color: #f8fafc; font-weight: bold; font-size: 0.8rem;";
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
                    foreach (var x in sGroup)
                    {
                        var item = x.Item;
                        var mat = materials.FirstOrDefault(m => m.Id == item.MaterialId);
                        decimal rowSubtotal = (decimal)item.Qty * item.CostPrice;
                        supplierSubtotal += rowSubtotal;
                        grandTotal += rowSubtotal;

                        sb.Append("<tr>");
                        sb.Append($"<td class='center'>{i}</td>");
                        sb.Append($"<td class='center'>#{po.Id:D5}</td>");
                        sb.Append($"<td class='center'>{po.Timestamp:dd/MM/yyyy}</td>");
                        sb.Append($"<td>{mat?.Name ?? "N/A"}</td>");
                        sb.Append($"<td class='center'>{mat?.Unit ?? "N/A"}</td>");
                        sb.Append($"<td>{(item.LotNumber == "Mặc định" ? "" : item.LotNumber)}</td>");
                        sb.Append($"<td class='center'>{item.Qty:0.##}</td>");
                        sb.Append($"<td class='number'>{item.CostPrice:N0}</td>");
                        sb.Append($"<td class='number'>{rowSubtotal:N0}</td>");
                        sb.Append("</tr>");
                        i++;
                    }
                    sb.Append($"<tr><td colspan='7' style='border:none;'></td><td class='footer' style='text-align:right;'>CỘNG {supplierName}:</td><td class='footer' style='text-align:right;'>{supplierSubtotal:N0}</td></tr>");
                }
                sb.Append("<tr><td colspan='9' style='border:none; height:10px;'></td></tr>");
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

        protected async Task SelectAndLinkSupplierAsync(PurchaseOrderItem item, Material material, int supplierId)
        {
            item.SupplierId = supplierId;

            if (material != null)
            {
                // Kiểm tra xem đã có link chưa
                bool alreadyLinked = material.MaterialSuppliers?.Any(ms => ms.SupplierId == supplierId) ?? false;

                if (!alreadyLinked)
                {
                    await WarehouseSvc.LinkMaterialToSupplierAsync(material.Id, supplierId);
                    // Refresh danh sách material để UI cập nhật các nút bấm NCC
                    materials = await WarehouseSvc.GetMasterMaterialsAsync(includeDeleted: true);

                    // Cập nhật tham chiếu material trong item hiện tại
                    item.Material = materials.FirstOrDefault(m => m.Id == item.MaterialId);
                }
            }

            CalculateTotal();
            StateHasChanged();
        }
    }
}
