using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using KhoHang.Models;
using KhoHang.Data;
using Microsoft.EntityFrameworkCore;

namespace KhoHang.Services
{
    public class MlForecastService
    {
        private readonly WarehouseService _warehouseService;
        private readonly IDbContextFactory<KhoDbContext> _dbFactory;

        public MlForecastService(WarehouseService warehouseService, IDbContextFactory<KhoDbContext> dbFactory)
        {
            _warehouseService = warehouseService;
            _dbFactory = dbFactory;
        }

        public class MaterialSalesData
        {
            public float Quantity { get; set; }
        }

        public class MaterialSalesForecast
        {
            public float[] ForecastedQuantity { get; set; } = Array.Empty<float>();
            public float[] ConfidenceLowerBounds { get; set; } = Array.Empty<float>();
            public float[] ConfidenceUpperBounds { get; set; } = Array.Empty<float>();
        }

        public class ForecastResult
        {
            public DateTime Date { get; set; }
            public float Quantity { get; set; }
            public float MinQuantity { get; set; }
            public float MaxQuantity { get; set; }
        }

        /// <summary>
        /// Dự báo nhu cầu vật tư trong horizonDays tiếp theo dựa trên lịch sử xuất kho cục bộ.
        /// </summary>
        public async Task<List<ForecastResult>> ForecastDemandAsync(int materialId, int horizonDays = 15)
        {
            // 1. Lấy dữ liệu lịch sử xuất kho
            var deliveries = await _warehouseService.GetMaterialDeliveryHistoryAsync(materialId);

            // Cần tối thiểu 5 điểm dữ liệu có hoạt động xuất kho để huấn luyện
            if (deliveries == null || deliveries.Count < 5)
            {
                return new List<ForecastResult>();
            }

            // Group theo ngày và tính tổng số lượng
            var groupedData = deliveries
                .GroupBy(d => d.Date.Date)
                .Select(g => new { Date = g.Key, Qty = (float)g.Sum(x => x.Qty) })
                .OrderBy(x => x.Date)
                .ToList();

            // Tạo chuỗi thời gian liên tục từ ngày đầu tiên đến hiện tại để tránh đứt gãy dữ liệu
            var startDate = groupedData.First().Date;
            var endDate = DateTime.Today;
            
            var totalDays = (endDate - startDate).Days + 1;
            if (totalDays < 10) totalDays = 10; // Đảm bảo độ dài chuỗi tối thiểu

            var timeSeries = new List<MaterialSalesData>();
            var dateMap = groupedData.ToDictionary(x => x.Date, x => x.Qty);

            for (int i = 0; i < totalDays; i++)
            {
                var curDate = startDate.AddDays(i);
                timeSeries.Add(new MaterialSalesData
                {
                    Quantity = dateMap.TryGetValue(curDate, out var qty) ? qty : 0f
                });
            }

            try
            {
                // 2. Thiết lập ML.NET TimeSeries SSA
                var mlContext = new MLContext();
                var dataView = mlContext.Data.LoadFromEnumerable(timeSeries);

                // WindowSize đại diện cho chu kỳ tuần hoàn (chọn 7 ngày hoặc 1/3 chuỗi thời gian)
                int windowSize = Math.Max(2, Math.Min(7, timeSeries.Count / 3));
                int seriesLength = timeSeries.Count;

                var forecastingPipeline = mlContext.Forecasting.ForecastBySsa(
                    outputColumnName: nameof(MaterialSalesForecast.ForecastedQuantity),
                    inputColumnName: nameof(MaterialSalesData.Quantity),
                    windowSize: windowSize,
                    seriesLength: seriesLength,
                    trainSize: seriesLength,
                    horizon: horizonDays,
                    confidenceLevel: 0.90f,
                    confidenceLowerBoundColumn: nameof(MaterialSalesForecast.ConfidenceLowerBounds),
                    confidenceUpperBoundColumn: nameof(MaterialSalesForecast.ConfidenceUpperBounds)
                );

                // 3. Huấn luyện mô hình (Model Training)
                var forecaster = forecastingPipeline.Fit(dataView);

                // 4. Dự đoán tương lai
                var forecastingEngine = forecaster.CreateTimeSeriesEngine<MaterialSalesData, MaterialSalesForecast>(mlContext);
                var forecast = forecastingEngine.Predict();

                // 5. Tổng hợp kết quả dự đoán
                var results = new List<ForecastResult>();
                var forecastStartDate = DateTime.Today.AddDays(1);

                for (int i = 0; i < horizonDays; i++)
                {
                    results.Add(new ForecastResult
                    {
                        Date = forecastStartDate.AddDays(i),
                        Quantity = Math.Max(0f, forecast.ForecastedQuantity[i]),
                        MinQuantity = Math.Max(0f, forecast.ConfidenceLowerBounds[i]),
                        MaxQuantity = Math.Max(0f, forecast.ConfidenceUpperBounds[i])
                    });
                }

                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error training ML.NET model for material {materialId}: {ex.Message}");
                return new List<ForecastResult>();
            }
        }

        public class RecommendedMaterialDto
        {
            public int MaterialId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Unit { get; set; } = string.Empty;
            public string? ProductCode { get; set; }
            public double Score { get; set; }
        }

        /// <summary>
        /// Gợi ý các vật tư đi kèm dựa trên phân tích các dự án trong quá khứ.
        /// </summary>
        public async Task<List<RecommendedMaterialDto>> GetCompanionMaterialsAsync(List<int> currentMaterialIds, int limit = 5)
        {
            if (currentMaterialIds == null || !currentMaterialIds.Any())
            {
                return new List<RecommendedMaterialDto>();
            }

            using var context = _dbFactory.CreateDbContext();

            // 1. Lấy tất cả các dự án có chứa ít nhất một trong các vật tư đang chọn
            var projectIdsWithTargetMaterials = await context.ProjectMaterials
                .Where(pm => currentMaterialIds.Contains(pm.MaterialId))
                .Select(pm => pm.ProjectId)
                .Distinct()
                .ToListAsync();

            if (!projectIdsWithTargetMaterials.Any())
            {
                return new List<RecommendedMaterialDto>();
            }

            // 2. Lấy tất cả vật tư của các dự án này (trừ các vật tư hiện tại đang được chọn)
            var allCompanionMaterials = await context.ProjectMaterials
                .Where(pm => projectIdsWithTargetMaterials.Contains(pm.ProjectId) && !currentMaterialIds.Contains(pm.MaterialId))
                .Select(pm => new { pm.MaterialId, pm.Name, pm.Unit, pm.Material.ProductCode })
                .ToListAsync();

            // 3. Đếm tần suất xuất hiện đồng thời và lấy danh sách xếp hạng
            var coOccurrenceCounts = allCompanionMaterials
                .GroupBy(m => m.MaterialId)
                .Select(g => new RecommendedMaterialDto
                {
                    MaterialId = g.Key,
                    Name = g.First().Name,
                    Unit = g.First().Unit,
                    ProductCode = g.First().ProductCode,
                    Score = g.Count()
                })
                .OrderByDescending(x => x.Score)
                .Take(limit)
                .ToList();

            return coOccurrenceCounts;
        }
    }
}
