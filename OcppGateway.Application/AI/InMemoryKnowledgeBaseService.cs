using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OcppGateway.Application.AI
{
    public interface IKnowledgeBaseService
    {
        Task<string> SearchManualsAsync(string query);
    }

    public class InMemoryKnowledgeBaseService : IKnowledgeBaseService
    {
        private readonly Dictionary<string, string> _manuals = new()
        {
            { "error 404", "Error 404: Conexión perdida con el servidor central. Verificar cable ethernet y configuración de firewall." },
            { "groundfault", "GroundFault: Falla de tierra detectada. Peligro de descarga. El cargador se ha bloqueado por seguridad. Requiere revisión física." },
            { "overcurrent", "OverCurrent: Corriente excedió el límite configurado. Verificar si el vehículo está solicitando más de lo permitido." },
            { "temperatura", "HighTemperature: El sensor interno detecta > 85°C. Reducir potencia a 50% automáticamente." },
            { "reset", "Para reiniciar el cargador, mantenga presionado el botón lateral por 5 segundos o envíe un comando RemoteReset." }
        };

        public Task<string> SearchManualsAsync(string query)
        {
            var results = _manuals
                .Where(k => query.ToLower().Contains(k.Key) || k.Value.ToLower().Contains(query.ToLower()))
                .Select(k => $"- {k.Value}")
                .Take(3);

            var context = string.Join("\n", results);
            return Task.FromResult(string.IsNullOrEmpty(context) ? "No se encontró información específica en los manuales." : context);
        }
    }
}
