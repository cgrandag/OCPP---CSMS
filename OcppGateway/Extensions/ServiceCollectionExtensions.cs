using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OcppGateway.Domain;
using OcppGateway.Infrastructure;
using OcppGateway.Infrastructure.Adapters;
using OcppGateway.Infrastructure.Persistence;
using OcppGateway.Infrastructure.Workers;
using OcppGateway.Infrastructure.Messaging;
using OcppGateway.Application;
using OcppGateway.Application.AI;
using OcppGateway.Application.Messaging;

namespace OcppGateway.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("PostgresDb") 
                ?? throw new InvalidOperationException("Connection string 'PostgresDb' not found.");
            
            services.AddDbContext<PostgresDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<IChargerRepository, PostgresChargerRepository>();
            services.AddScoped<IUserRepository, PostgresUserRepository>();

            string redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "OcppGateway_";
            });

            services.AddSingleton<ConnectionManager>();
            services.AddSingleton<LogBroadcaster>();

            services.AddTransient<IChargerControlService, ChargerControlService>();

            services.AddHostedService<MonitoringWorker>();

            services.AddSingleton<IMessageProducer, RabbitMqProducer>();

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddTransient<IOcppConnectionService, OcppConnectionService>();
            services.AddTransient<IOcppValidationService, OcppValidationService>();
            services.AddTransient<WebSocketAdapter>();
            services.AddTransient<ISmartChargingService, SmartChargingService>();
            
            services.AddSingleton<ChargerStateNotifier>();
            
            return services;
        }

        public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
        {
            string geminiApiKey = configuration["Gemini:ApiKey"] 
                ?? throw new InvalidOperationException("Gemini API Key not found.");

            services.AddSingleton<IKnowledgeBaseService, InMemoryKnowledgeBaseService>();

            services.AddTransient<IChatbotService>(provider =>
                new GeminiAdapter(
                    geminiApiKey,
                    provider.GetRequiredService<IChargerRepository>(),      
                    provider.GetRequiredService<IChargerControlService>(),
                    provider.GetRequiredService<IKnowledgeBaseService>()
                ));

            return services;
        }

        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy => { policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); });
            });

            services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
            {
                options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            });

            var jwtKey = configuration["Jwt:Key"] ?? "SecretKeyVeryLongStringToSignJWTWithHMACSHA256";
            var jwtIssuer = configuration["Jwt:Issuer"] ?? "OcppGatewayIssuer";
            
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = false,
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.Name
                };
            });

            services.AddAuthorization();

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            return services;
        }
    }
}
