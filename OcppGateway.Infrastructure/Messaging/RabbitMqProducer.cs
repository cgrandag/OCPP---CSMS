using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using OcppGateway.Application.Messaging;

namespace OcppGateway.Infrastructure.Messaging
{
    public class RabbitMqProducer : IMessageProducer, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public RabbitMqProducer(IConfiguration configuration)
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMq:Host"] ?? "localhost",
                UserName = configuration["RabbitMq:Username"] ?? "guest",
                Password = configuration["RabbitMq:Password"] ?? "guest"
            };

            try 
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                
                _channel.QueueDeclare(queue: "meter_values", durable: true, exclusive: false, autoDelete: false, arguments: null);
                _channel.QueueDeclare(queue: "ocpp_events", durable: true, exclusive: false, autoDelete: false, arguments: null);
            }
            catch(Exception)
            {
                _connection = null;
                _channel = null;
            }
        }

        public void SendMessage<T>(T message, string queueName)
        {
            if (_channel == null) return;

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            _channel.BasicPublish(exchange: "",
                                 routingKey: queueName,
                                 basicProperties: null,
                                 body: body);
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
}
