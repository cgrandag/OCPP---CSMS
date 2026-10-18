using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OcppGateway.Domain;
using OcppGateway.Domain.Entities;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace OcppGateway.Endpoints
{
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this IEndpointRouteBuilder app, IConfiguration configuration)
        {
            var group = app.MapGroup("/api/auth");

            group.MapPost("/login", async (LoginRequest request, IUserRepository userRepo) =>
            {
                try
                {
                    var users = await userRepo.GetAllUsersAsync();
                    
                    if (!users.Any())
                    {
                        await userRepo.CreateUserAsync(new User
                        {
                            Username = "admin",
                            PasswordHash = "admin",
                            Email = "admin@example.com",
                            Role = "Super Admin"
                        });
                        users = await userRepo.GetAllUsersAsync();
                    }

                    var user = users.FirstOrDefault(u => u.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase));

                    if (user == null)
                    {
                        return Results.Unauthorized();
                    }

                    if (string.IsNullOrEmpty(user.PasswordHash))
                    {
                        user.PasswordHash = request.Password;
                        await userRepo.UpdateUserAsync(user.Id, user);
                    }
                    else if (user.PasswordHash != request.Password)
                    {
                        return Results.Unauthorized();
                    }

                    var jwtKey = configuration["Jwt:Key"] ?? "SecretKeyVeryLongStringToSignJWTWithHMACSHA256";
                    var jwtIssuer = configuration["Jwt:Issuer"] ?? "OcppGatewayIssuer";

                    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
                    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                    var claims = new[]
                    {
                        new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Role, user.Role),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                    };

                    var token = new JwtSecurityToken(
                        issuer: jwtIssuer,
                        audience: jwtIssuer,
                        claims: claims,
                        expires: DateTime.Now.AddHours(2),
                        signingCredentials: credentials);

                    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                    return Results.Ok(new { Token = tokenString, Role = user.Role, Username = user.Username });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AUTH ERROR]: {ex}");
                    return Results.Problem(ex.Message);
                }
            });
        }
    }
}
