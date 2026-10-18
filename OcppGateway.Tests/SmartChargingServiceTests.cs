using Moq;
using Microsoft.Extensions.Logging;
using OcppGateway.Application;
using OcppGateway.Domain;
using Xunit;

namespace OcppGateway.Tests
{
    public class SmartChargingServiceTests
    {
        private readonly Mock<IChargerControlService> _mockControlService;
        private readonly Mock<ILogger<SmartChargingService>> _mockLogger;
        private readonly SmartChargingService _service;

        public SmartChargingServiceTests()
        {
            _mockControlService = new Mock<IChargerControlService>();
            _mockLogger = new Mock<ILogger<SmartChargingService>>();
            _service = new SmartChargingService(_mockControlService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task AnalyzeMeterValuesAsync_ShouldStopTransaction_WhenSoCIsAboveLimit()
        {
            var chargePointId = "TEST001";
            var transactionId = 123;
            var meterValues = new[]
            {
                new MeterValue { Measurand = "SoC", Value = "96", Unit = "Percent" }
            };

            await _service.AnalyzeMeterValuesAsync(chargePointId, transactionId, meterValues);

            _mockControlService.Verify(x => x.SendStopTransactionAsync(chargePointId, transactionId), Times.Once);
        }

        [Fact]
        public async Task AnalyzeMeterValuesAsync_ShouldDoNothing_WhenSoCIsBelowLimit()
        {
            var chargePointId = "TEST001";
            var transactionId = 123;
            var meterValues = new[]
            {
                new MeterValue { Measurand = "SoC", Value = "80", Unit = "Percent" }
            };

            await _service.AnalyzeMeterValuesAsync(chargePointId, transactionId, meterValues);

            _mockControlService.Verify(x => x.SendStopTransactionAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task AnalyzeMeterValuesAsync_ShouldIgnore_WhenMeasurandIsNotSoC()
        {
            var chargePointId = "TEST001";
            var transactionId = 123;
            var meterValues = new[]
            {
                new MeterValue { Measurand = "Energy.Active.Import.Register", Value = "1000", Unit = "Wh" }
            };

            await _service.AnalyzeMeterValuesAsync(chargePointId, transactionId, meterValues);

            _mockControlService.Verify(x => x.SendStopTransactionAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task AnalyzeMeterValuesAsync_ShouldLogWarning_WhenTransactionIdIsMissing()
        {
            var chargePointId = "TEST001";
            int? transactionId = null; 
            var meterValues = new[]
            {
                new MeterValue { Measurand = "SoC", Value = "99", Unit = "Percent" }
            };

            await _service.AnalyzeMeterValuesAsync(chargePointId, transactionId, meterValues);

            _mockControlService.Verify(x => x.SendStopTransactionAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }
    }
}
