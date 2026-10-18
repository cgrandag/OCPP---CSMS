using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Authorization;
using OcppGateway.Domain;
using OcppGateway.Domain.Entities;
using System.Threading.Tasks;

namespace OcppGateway.Endpoints
{
    public static class AssetEndpoints
    {
        public static void MapAssetEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/assets")
                           .RequireAuthorization(policy => policy.RequireRole("Super Admin"));

            group.MapGet("/", async (IChargerRepository repo) =>
            {
                var chargers = await repo.GetAllChargersAsync();
                return Results.Ok(chargers);
            });

            group.MapGet("/{id}", async (string id, IChargerRepository repo) =>
            {
                var charger = await repo.GetChargerAsync(id);
                return charger is not null ? Results.Ok(charger) : Results.NotFound();
            });

            group.MapPost("/", async (Charger newCharger, IChargerRepository repo) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(newCharger.ChargePointId))
                    {
                        newCharger.ChargePointId = "CP-" + System.Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                    }
                    var createdCharger = await repo.CreateChargerAsync(newCharger);
                    return Results.Created($"/api/assets/{createdCharger.ChargePointId}", createdCharger);
                }
                catch (System.Exception ex)
                {
                    return Results.BadRequest(ex.InnerException?.Message ?? ex.Message);
                }
            });

            group.MapPut("/{id}", async (string id, Charger updatedCharger, IChargerRepository repo) =>
            {
                try
                {
                    var charger = await repo.UpdateChargerAsync(id, updatedCharger);
                    return charger is not null ? Results.Ok(charger) : Results.NotFound();
                }
                catch (System.Exception ex)
                {
                    return Results.BadRequest(ex.InnerException?.Message ?? ex.Message);
                }
            });

            group.MapDelete("/{id}", async (string id, IChargerRepository repo) =>
            {
                var deleted = await repo.DeleteChargerAsync(id);
                return deleted ? Results.NoContent() : Results.NotFound();
            });
        }
    }
}
