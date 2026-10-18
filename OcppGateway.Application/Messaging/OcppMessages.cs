using System.Text.Json.Serialization;

namespace OcppGateway.Application.Messaging
{
    public enum OcppMessageType
    {
        Call = 2,
        CallResult = 3,
        CallError = 4
    }

    public class BootNotificationRequest
    {
        [JsonPropertyName("chargePointVendor")]
        public string ChargePointVendor { get; set; } = string.Empty;

        [JsonPropertyName("chargePointModel")]
        public string ChargePointModel { get; set; } = string.Empty;

        [JsonPropertyName("chargePointSerialNumber")]
        public string? ChargePointSerialNumber { get; set; }

        [JsonPropertyName("firmwareVersion")]
        public string? FirmwareVersion { get; set; }
    }

    public class BootNotificationResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "Accepted";

        [JsonPropertyName("currentTime")]
        public string CurrentTime { get; set; } = DateTime.UtcNow.ToString("o");

        [JsonPropertyName("interval")]
        public int Interval { get; set; } = 300;
    }

    public class HeartbeatResponse
    {
        [JsonPropertyName("currentTime")]
        public string CurrentTime { get; set; } = DateTime.UtcNow.ToString("o");
    }

    public class StatusNotificationRequest
    {
        [JsonPropertyName("connectorId")]
        public int ConnectorId { get; set; }

        [JsonPropertyName("errorCode")]
        public string ErrorCode { get; set; } = "NoError";

        [JsonPropertyName("info")]
        public string? Info { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Available";

        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }

        [JsonPropertyName("vendorId")]
        public string? VendorId { get; set; }

        [JsonPropertyName("vendorErrorCode")]
        public string? VendorErrorCode { get; set; }
    }

    public class StartTransactionRequest
    {
        [JsonPropertyName("connectorId")]
        public int ConnectorId { get; set; }

        [JsonPropertyName("idTag")]
        public string IdTag { get; set; } = string.Empty;

        [JsonPropertyName("meterStart")]
        public int MeterStart { get; set; }

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = string.Empty;
    }

    public class StartTransactionResponse
    {
        [JsonPropertyName("transactionId")]
        public int TransactionId { get; set; }

        [JsonPropertyName("idTagInfo")]
        public IdTagInfo IdTagInfo { get; set; } = new();
    }

    public class StopTransactionRequest
    {
        [JsonPropertyName("idTag")]
        public string? IdTag { get; set; }

        [JsonPropertyName("meterStop")]
        public int MeterStop { get; set; }

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = string.Empty;

        [JsonPropertyName("transactionId")]
        public int TransactionId { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }
    }

    public class StopTransactionResponse
    {
        [JsonPropertyName("idTagInfo")]
        public IdTagInfo? IdTagInfo { get; set; }
    }

    public class MeterValuesRequest
    {
        [JsonPropertyName("connectorId")]
        public int ConnectorId { get; set; }

        [JsonPropertyName("transactionId")]
        public int? TransactionId { get; set; }

        [JsonPropertyName("meterValue")]
        public MeterValueItem[] MeterValue { get; set; } = Array.Empty<MeterValueItem>();
    }

    public class MeterValueItem
    {
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = string.Empty;

        [JsonPropertyName("sampledValue")]
        public SampledValue[] SampledValue { get; set; } = Array.Empty<SampledValue>();
    }

    public class SampledValue
    {
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("context")]
        public string? Context { get; set; }

        [JsonPropertyName("format")]
        public string? Format { get; set; }

        [JsonPropertyName("measurand")]
        public string? Measurand { get; set; }

        [JsonPropertyName("phase")]
        public string? Phase { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }
    }

    public class IdTagInfo
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "Accepted";

        [JsonPropertyName("expiryDate")]
        public string? ExpiryDate { get; set; }
    }

    public class AuthorizeRequest
    {
        [JsonPropertyName("idTag")]
        public string IdTag { get; set; } = string.Empty;
    }

    public class AuthorizeResponse
    {
        [JsonPropertyName("idTagInfo")]
        public IdTagInfo IdTagInfo { get; set; } = new();
    }
}
