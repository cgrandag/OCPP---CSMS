using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using OcppGateway.Domain;
using OcppGateway.Infrastructure.Persistence;
using System.Linq;

namespace OcppGateway.Endpoints
{
    public static class TariffEndpoints
    {
        public static void MapTariffEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/tariffs")
                           .RequireAuthorization(policy => policy.RequireRole("Super Admin", "Contabilidad"));

            group.MapGet("/", async (PostgresDbContext db) =>
            {
                var tariffs = await db.Tariffs.OrderBy(t => t.Id).ToListAsync();
                return Results.Ok(tariffs);
            });

            group.MapGet("/{id:int}", async (int id, PostgresDbContext db) =>
            {
                var tariff = await db.Tariffs.FindAsync(id);
                return tariff is not null ? Results.Ok(tariff) : Results.NotFound();
            });

            group.MapPost("/", async (Tariff newTariff, PostgresDbContext db) =>
            {
                db.Tariffs.Add(newTariff);
                await db.SaveChangesAsync();
                return Results.Created($"/api/tariffs/{newTariff.Id}", newTariff);
            });

            group.MapPut("/{id:int}", async (int id, Tariff updatedTariff, PostgresDbContext db) =>
            {
                var tariff = await db.Tariffs.FindAsync(id);
                if (tariff == null) return Results.NotFound();

                tariff.Name = updatedTariff.Name;
                tariff.PricePerKwh = updatedTariff.PricePerKwh;
                tariff.PricePerMinute = updatedTariff.PricePerMinute;
                tariff.FlatFee = updatedTariff.FlatFee;

                await db.SaveChangesAsync();
                return Results.Ok(tariff);
            });

            group.MapDelete("/{id:int}", async (int id, PostgresDbContext db) =>
            {
                var tariff = await db.Tariffs.FindAsync(id);
                if (tariff == null) return Results.NotFound();

                db.Tariffs.Remove(tariff);
                await db.SaveChangesAsync();
                return Results.NoContent();
            });
        }
    }
}
