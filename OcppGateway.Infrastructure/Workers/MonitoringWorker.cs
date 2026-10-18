using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OcppGateway.Domain;
using System.Text.Json.Nodes;
using OcppGateway.Application;

namespace OcppGateway.Infrastructure.Workers
{
    public class MonitoringWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MonitoringWorker> _logger;

        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

        public MonitoringWorker(IServiceProvider serviceProvider, ILogger<MonitoringWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Iniciando servicio de monitoreo...");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var repository = scope.ServiceProvider.GetRequiredService<IChargerRepository>();
                            var aiAgent = scope.ServiceProvider.GetRequiredService<IChatbotService>();

                            var targetId = "879280";

                            var statusText = await repository.GetLastStatusAsync(targetId);

                            if (statusText.Contains("Error:") && !statusText.Contains("NoError"))
                            {
                                var parts = statusText.Split(new[] { "(Error:", ")" }, StringSplitOptions.RemoveEmptyEntries);
                                var estado = parts[0].Trim();
                                var error = parts.Length > 1 ? parts[1].Trim() : "Unknown";

                                _logger.LogWarning("Anomalía detectada en {Id}. Generando diagnóstico...", targetId);

                                var reporteJson = await aiAgent.AnalyzeIssueAsync(targetId, estado, error);

                                try
                                {
                                    var decision = JsonNode.Parse(reporteJson);
                                    string severidad = decision?["severidad"]?.ToString() ?? "INFO";
                                    string accion = decision?["accion_recomendada"]?.ToString() ?? "Revisar";

                                    if (severidad == "CRITICA")
                                    {
                                        _logger.LogError("[DIAGNÓSTICO] {Id}: {Diagnostico} | ACCIÓN: {Accion}",
                                            targetId, decision?["diagnostico"], accion);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("[DIAGNÓSTICO] {Id}: {Diagnostico} | Sugerencia: {Accion}",
                                            targetId, decision?["diagnostico"], accion);
                                    }
                                }
                                catch
                                {
                                    _logger.LogInformation("Diagnóstico: {Msg}", reporteJson);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error en el ciclo de monitoreo.");
                    }

                    await Task.Delay(_checkInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Servicio de monitoreo detenido.");
            }
        }
    }
}