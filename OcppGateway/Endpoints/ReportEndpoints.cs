using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OcppGateway.Application.Dtos;
using OcppGateway.Domain;
using System;
using System.Linq;

namespace OcppGateway.Endpoints
{
    public static class ReportEndpoints
    {
        public static void MapReportEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/reports/overview", async (IChargerRepository repo) =>
            {
                var chargers = await repo.GetAllChargersAsync();
                var transactions = await repo.GetAllTransactionsAsync();

                var statusCounts = chargers
                    .GroupBy(c => c.Status)
                    .ToDictionary(g => g.Key, g => g.Count());

                var totalFees = transactions.Sum(t => t.TotalCost);
                var totalKwh = transactions.Sum(t => Math.Max(0, (t.StopMeterValue - t.StartMeterValue) / 1000m));
                
                var monthlyStats = transactions
                    .GroupBy(t => new { t.StartTime.Year, t.StartTime.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Select(g => new {
                        Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                        Fees = g.Sum(t => t.TotalCost),
                        Energy = g.Sum(t => Math.Max(0, (t.StopMeterValue - t.StartMeterValue) / 1000m)),
                        Sessions = g.Count()
                    }).ToList();

                var recentSessions = transactions
                    .OrderByDescending(t => t.StartTime)
                    .Take(5)
                    .Select(t => new {
                        AuthType = "RFID",
                        Location = t.Charger?.Location ?? "Desconocido",
                        ChargerId = t.ChargePointId,
                        StartTime = t.StartTime,
                        Status = t.StopTime.HasValue ? "Completed" : "Active",
                        EnergyKwh = Math.Round(Math.Max(0, (t.StopMeterValue - t.StartMeterValue) / 1000m), 2),
                        Cost = Math.Round(t.TotalCost, 2)
                    }).ToList();

                var overview = new
                {
                    ChargerStatus = statusCounts,
                    FeesCollected = totalFees,
                    EnergyUsedKwh = Math.Round(totalKwh, 2),
                    TotalSessions = transactions.Count(),
                    MonthlyStats = monthlyStats,
                    RecentSessions = recentSessions
                };

                return Results.Ok(overview);
            })
            .WithTags("Reporting");

            app.MapGet("/api/reports/history/{chargePointId}", async (string chargePointId, IChargerRepository repo) =>
            {
                var transactions = await repo.GetTransactionsByChargerAsync(chargePointId);

                var report = transactions.Select(t =>
                {
                    double kwh = 0;
                    if (t.StopMeterValue > t.StartMeterValue)
                    {
                        kwh = (t.StopMeterValue - t.StartMeterValue) / 1000.0;
                    }

                    string duration = "En curso";
                    if (t.StopTime.HasValue)
                    {
                        var timeSpan = t.StopTime.Value - t.StartTime;
                        duration = $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
                    }

                    return new TransactionReportDto
                    {
                        TransactionId = t.TransactionId,
                        ChargePointId = t.ChargePointId,
                        IdTag = t.IdTag,
                        StartTime = t.StartTime,
                        StopTime = t.StopTime,
                        Duration = duration,
                        TotalKw = Math.Round(kwh, 2),
                        StopReason = t.StopReason ?? "N/A",
                        Status = t.StopTime.HasValue ? "Completed" : "Active"
                    };
                });

                return Results.Ok(report);
            })
            .WithTags("Reporting");
        }
    }
}
