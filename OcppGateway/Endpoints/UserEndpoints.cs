using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OcppGateway.Domain;
using OcppGateway.Domain.Entities;
using System.Threading.Tasks;

namespace OcppGateway.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/users")
                           .RequireAuthorization(policy => policy.RequireRole("Super Admin"));

            group.MapGet("/", async (IUserRepository repo) =>
            {
                var users = await repo.GetAllUsersAsync();
                return Results.Ok(users);
            });

            group.MapGet("/{id:int}", async (int id, IUserRepository repo) =>
            {
                var user = await repo.GetUserByIdAsync(id);
                return user is not null ? Results.Ok(user) : Results.NotFound();
            });

            group.MapPost("/", async (User newUser, IUserRepository repo) =>
            {
                var createdUser = await repo.CreateUserAsync(newUser);
                return Results.Created($"/api/users/{createdUser.Id}", createdUser);
            });

            group.MapPut("/{id:int}", async (int id, User updatedUser, IUserRepository repo) =>
            {
                var user = await repo.UpdateUserAsync(id, updatedUser);
                return user is not null ? Results.Ok(user) : Results.NotFound();
            });

            group.MapDelete("/{id:int}", async (int id, IUserRepository repo) =>
            {
                var deleted = await repo.DeleteUserAsync(id);
                return deleted ? Results.NoContent() : Results.NotFound();
            });
        }
    }
}
