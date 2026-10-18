using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Moq;
using OcppGateway.Application;
using OcppGateway.Application.Messaging;
using OcppGateway.Domain;
using Xunit;

namespace OcppGateway.Tests
{
    public class OcppConnectionServiceTests
    {
        private readonly Mock<IChargerRepository> _mockRepo;
        private readonly Mock<IChargerControlService> _mockControl;
        private readonly Mock<IMessageProducer> _mockProducer;
        private readonly Mock<ILogger<OcppConnectionService>> _mockLogger;
        private readonly Mock<ISmartChargingService> _mockSmartCharging;
        private readonly Mock<IOcppValidationService> _mockValidation;
        private readonly OcppConnectionService _service;

        public OcppConnectionServiceTests()
        {
            _mockRepo = new Mock<IChargerRepository>();
            _mockControl = new Mock<IChargerControlService>();
            _mockProducer = new Mock<IMessageProducer>();
            _mockLogger = new Mock<ILogger<OcppConnectionService>>();
            _mockSmartCharging = new Mock<ISmartChargingService>();
            _mockValidation = new Mock<IOcppValidationService>();

            _mockValidation.Setup(v => v.IsIdTagAuthorizedAsync(It.IsAny<string>())).ReturnsAsync(true);

            _service = new OcppConnectionService(
                _mockRepo.Object,
                _mockLogger.Object,
                _mockControl.Object,
                _mockProducer.Object,
                _mockSmartCharging.Object,
                _mockValidation.Object
            );
        }

        [Fact]
        public async Task ProcessMessageAsync_BootNotification_ShouldReturnAccepted()
        {
            string chargePointId = "CP-100";
            var bootPayload = new
            {
                chargePointVendor = "VendorX",
                chargePointModel = "ModelY"
            };
            
            var ocppMsg = new object[] { 2, "msg-123", "BootNotification", bootPayload };
            string validJson = JsonSerializer.Serialize(ocppMsg);

            string? resultP = await _service.ProcessMessageAsync(chargePointId, validJson);

            Assert.NotNull(resultP);
            var node = JsonNode.Parse(resultP!);
            Assert.NotNull(node);
            var arr = node.AsArray();
            Assert.Equal(3, arr.Count);
            Assert.Equal(3, arr[0]?.GetValue<int>());
            Assert.Equal("msg-123", arr[1]?.GetValue<string>());

            var payload = arr[2];
            Assert.Equal("Accepted", payload?["status"]?.GetValue<string>());
            
            _mockRepo.Verify(r => r.SaveStatusAsync(chargePointId, "Booting", "NoError", "Device Booted"), Times.Once);
        }

        [Fact]
        public async Task ProcessMessageAsync_Heartbeat_ShouldUpdateLastSeen()
        {
            string chargePointId = "CP-100";
            var ocppMsg = new object[] { 2, "msg-hb", "Heartbeat", new { } };
            string validJson = JsonSerializer.Serialize(ocppMsg);

            await _service.ProcessMessageAsync(chargePointId, validJson);

            _mockRepo.Verify(r => r.SaveStatusAsync(chargePointId, "Online", "NoError", ""), Times.Once);
        }
    }
}
