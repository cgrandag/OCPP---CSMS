namespace OcppGateway.Application
{
    public interface IChatbotService
    {
        Task<string> AskAssistantAsync(string chargePointId, string userQuestion);
        Task<string> AnalyzeIssueAsync(string chargePointId, string status, string errorCode);
    }
}