using Microsoft.AspNetCore.Components;
using KhoHang.Models;
using KhoHang.Services;

namespace KhoHang.Components.Pages;

public partial class Retail : ComponentBase
{
    private string activeTab = "create";

    // Create Mode
    private RetailOrder newOrder = new();
    private List<RetailOrderItem> draftItems = new();
    private Dictionary<int, List<MaterialLot>> availableLots = new();

    private decimal totalAmount = 0;
    private decimal amountPaid = 0;

    private bool isSelectionModalOpen = false;
    private bool isProcessing = false;
    private bool showToast = false;
    private string toastMessage = "";

    // History Mode
    private DateTime? filterFrom = DateTime.Today.AddDays(-30);
    private DateTime? filterTo = DateTime.Today;
    private string filterCustomer = "";
    private List<RetailOrder>? historyOrders = null;
    private RetailOrder? selectedOrder = null;

    private List<Customer> customers = new();

    protected override async Task OnInitializedAsync()
    {
        customers = await WarehouseService.GetCustomersAsync();
    }

    private void OnCustomerSelected(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int id))
        {
            var c = customers.FirstOrDefault(x => x.Id == id);
            if (c != null)
            {
                newOrder.CustomerName = c.Name;
                newOrder.Phone = c.Phone;
                newOrder.Address = c.Address;
            }
        }
    }

    private void UpdateTotals()
    {
        totalAmount = draftItems.Sum(x => (decimal)x.Qty * x.Price);
        if (amountPaid < totalAmount) amountPaid = totalAmount; // Default to full amount
    }

    private async Task OnMaterialsSelected(List<Material> selectedMaterials)
    {
        foreach (var mat in selectedMaterials)
        {
            if (!draftItems.Any(i => i.MaterialId == mat.Id))
            {
                var lots = await WarehouseService.GetMaterialLotsAsync(mat.Id);
                availableLots[mat.Id] = lots;

                // Pick best lot
                string lotNum = "";
                decimal price = 0;
                var validLots = lots.Where(l => l.StockQty > 0).ToList();
                if (validLots.Any())
                {
                    var bestLot = validLots.OrderBy(l => l.Id).First();
                    lotNum = bestLot.LotNumber;
                    price = bestLot.BasePrice > 0 ? bestLot.BasePrice : bestLot.CostPrice * 1.2m;
                }

                draftItems.Add(new RetailOrderItem
                {
                    MaterialId = mat.Id,
                    Name = mat.Name,
                    Unit = mat.Unit,
                    Qty = 1,
                    Price = price,
                    LotNumber = lotNum
                });
            }
        }
        UpdateTotals();
        StateHasChanged();
    }

    private void ShowError(string msg)
    {
        toastMessage = msg;
        showToast = true;
        StateHasChanged();
        _ = Task.Delay(5000).ContinueWith(_ => { showToast = false; InvokeAsync(StateHasChanged); });
    }

    private void DuplicateItem(RetailOrderItem item)
    {
        var lots = availableLots.ContainsKey(item.MaterialId) ? availableLots[item.MaterialId] : new List<MaterialLot>();

        // Find next unused lot if possible
        var usedLots = draftItems
            .Where(x => x.MaterialId == item.MaterialId)
            .Select(x => x.LotNumber)
            .ToList();

        var nextLot = lots.FirstOrDefault(l => !usedLots.Contains(l.LotNumber) && l.StockQty > 0);

        if (nextLot == null)
        {
            ShowError($"Vật tư '{item.Name}' đã hết lô khả dụng khác!");
        }

        var index = draftItems.IndexOf(item);
        draftItems.Insert(index + 1, new RetailOrderItem
        {
            MaterialId = item.MaterialId,
            Name = item.Name,
            Unit = item.Unit,
            Qty = 1,
            Price = item.Price,
            LotNumber = nextLot?.LotNumber ?? ""
        });

        UpdateTotals();
        StateHasChanged();
    }

    private async Task SubmitOrder()
    {
        if (string.IsNullOrWhiteSpace(newOrder.CustomerName))
        {
            ShowError("Vui lòng nhập tên khách hàng!");
            return;
        }

        if (!draftItems.Any())
        {
            ShowError("Chưa chọn hàng hóa nào!");
            return;
        }

        // Kiểm tra trùng lô cho cùng 1 vật tư
        var groupedByMat = draftItems.GroupBy(x => x.MaterialId);
        foreach (var group in groupedByMat)
        {
            var duplicates = group.GroupBy(x => x.LotNumber)
                                  .Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key))
                                  .Select(g => g.Key)
                                  .ToList();
            if (duplicates.Any())
            {
                var matName = group.First().Name;
                ShowError($"Vật tư '{matName}' bị trùng lô '{duplicates.First()}'! Vui lòng chọn lô khác.");
                return;
            }
        }

        foreach (var item in draftItems)
        {
            if (item.Qty <= 0)
            {
                ShowError($"Số lượng của '{item.Name}' phải lớn hơn 0!");
                return;
            }
            if (string.IsNullOrEmpty(item.LotNumber))
            {
                ShowError($"Vui lòng chọn lô cho '{item.Name}'!");
                return;
            }

            // Check stock
            var lots = availableLots[item.MaterialId];
            var lot = lots.FirstOrDefault(l => l.LotNumber == item.LotNumber);
            if (lot == null || lot.StockQty < item.Qty)
            {
                ShowError($"Lô '{item.LotNumber}' của '{item.Name}' không đủ tồn kho (Còn: {lot?.StockQty ?? 0})!");
                return;
            }
        }

        isProcessing = true;
        StateHasChanged();

        try
        {
            newOrder.TotalAmount = totalAmount;
            newOrder.AmountPaid = amountPaid;
            newOrder.Items = draftItems.ToList();
            newOrder.Timestamp = DateTime.Now;

            foreach (var item in newOrder.Items)
            {
                item.Subtotal = (decimal)item.Qty * item.Price;
            }

            var newId = await WarehouseService.CreateRetailOrderAsync(newOrder);
            newOrder.Id = newId;
            var printedOrder = newOrder;

            // Reset form
            newOrder = new RetailOrder();
            draftItems.Clear();
            availableLots.Clear();
            totalAmount = 0;
            amountPaid = 0;

            activeTab = "history";
            await LoadHistoryAsync();

            selectedOrder = historyOrders?.FirstOrDefault(o => o.Id == printedOrder.Id) ?? printedOrder;
        }
        catch (Exception ex)
        {
            ShowError($"Lỗi hệ thống: {ex.Message}");
        }
        finally
        {
            isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task LoadHistoryAsync()
    {
        historyOrders = null; // show loading
        StateHasChanged();

        historyOrders = await WarehouseService.GetRetailOrdersAsync(filterFrom, filterTo, filterCustomer);
        StateHasChanged();
    }

    private void FilterHistory()
    {
        _ = LoadHistoryAsync();
    }

    private void ViewOrderDetails(RetailOrder order)
    {
        selectedOrder = order;
        StateHasChanged();
    }
}
