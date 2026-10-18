using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using OcppGateway.Application;
using OcppGateway.Application.AI;
using OcppGateway.Domain;

namespace OcppGateway.Infrastructure.Adapters
{
    public class GeminiAdapter : IChatbotService
    {
        private readonly string _apiKey;
        private readonly IChargerRepository _repository;
        private readonly IChargerControlService _controlService;
        private readonly OcppGateway.Application.AI.IKnowledgeBaseService _knowledgeBase;
        private readonly HttpClient _httpClient;

        public GeminiAdapter(string apiKey, 
            IChargerRepository repository, 
            IChargerControlService controlService,
            OcppGateway.Application.AI.IKnowledgeBaseService knowledgeBase)
        {
            _apiKey = apiKey;
            _repository = repository;
            _controlService = controlService;
            _knowledgeBase = knowledgeBase;
            _httpClient = new HttpClient();
        }
        public async Task<string> AnalyzeIssueAsync(string chargePointId, string status, string errorCode)
        {
            string contextDocs = await _knowledgeBase.SearchManualsAsync(errorCode + " " + status);

            var prompt = $@"
            ACTÚA COMO: Ingeniero Senior de Soporte Técnico.
            OBJETIVO: Diagnosticar falla en cargador OCPP.
    
            CONTEXTO TÉCNICO (MANUALES):
            {contextDocs}

            DATOS DEL INCIDENTE:
            - ID Cargador: {chargePointId}
            - Estado Actual: {status}
            - Código de Error: {errorCode}
    
            TU TAREA:
            Responde ÚNICAMENTE en formato JSON:
            {{
            ""severidad"": ""BAJA"" | ""MEDIA"" | ""CRITICA"",
            ""diagnostico"": ""Explicación técnica basada en el contexto"",
            ""accion_recomendada"": ""Paso a paso para solucionar""
            }}";

            var jsonResponse = await CallGeminiApi(prompt);

            return jsonResponse.Replace("```json", "").Replace("```", "").Trim();
        }

        public async Task<string> AskAssistantAsync(string chargePointId, string userQuestion)
        {
            var status = await _repository.GetLastStatusAsync(chargePointId) ?? "Unknown";

            var prompt = $@"
            Eres un asistente técnico para gestión y soporte de cargadores OCPP.
            
            DATOS EN TIEMPO REAL:
            - ID Cargador: {chargePointId}
            - Estado Actual: {status}
            
            SOLICITUD DEL USUARIO: ""{userQuestion}""

            TUS PROTOCOLOS DE ACCIÓN:
            1. Si piden INICIAR carga, responde: [CMD_START]
            2. Si piden DETENER carga, responde: [CMD_STOP]
            3. Si no, responde técnicamente.";

            var responseText = await CallGeminiApi(prompt);

            if (responseText.Contains("[CMD_START]"))
            {
                if (status.Contains("Charging")) return "El cargador ya se encuentra en proceso de carga.";

                await _controlService.SendStartTransactionAsync(chargePointId);
                return "Iniciando secuencia de carga remota...";
            }

            if (responseText.Contains("[CMD_STOP]"))
            {
                await _controlService.SendStopTransactionAsync(chargePointId, 1001);
                return "Deteniendo el suministro de energía.";
            }

            return responseText;
        }

        private async Task<string> CallGeminiApi(string prompt)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            var payload = new
            {
                contents = new[] {
            new { parts = new[] { new { text = prompt } } }
        }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    return $"ERROR API ({response.StatusCode}): {errorDetails}";
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResponse);

                return doc.RootElement.GetProperty("candidates")[0]
                                    .GetProperty("content")
                                    .GetProperty("parts")[0]
                                    .GetProperty("text").GetString() ?? "";
            }
            catch (Exception ex)
            {
                return $"EXCEPCIÓN LOCAL: {ex.Message}";
            }
        }
    }
}