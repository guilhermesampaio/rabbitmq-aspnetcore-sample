using BuildingBlock.EventBus;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace BuildingBlock.RabbitMQ
{
    public class RabbitMqEventBus : IEventBus, IDisposable
    {
        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly RabbitMQOptions _rabbitMQOptions;
        private IModel _consumerChannel;

        public RabbitMqEventBus(IRabbitMQPersistentConnection persistentConnection, IOptions<RabbitMQOptions> rabbitMQOptions)
        {
            _persistentConnection = persistentConnection;
            _rabbitMQOptions = rabbitMQOptions?.Value;
        }

        public void Publish(IntegrationEvent @event)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            using (var channel = _persistentConnection.CreateModel())
            {
                var eventName = @event.GetType().Name;
                channel.ExchangeDeclare(exchange: _rabbitMQOptions.ExchangeName, type: "direct");
                var message = JsonConvert.SerializeObject(@event);
                var body = Encoding.UTF8.GetBytes(message);
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2;
                channel.BasicPublish(
                    exchange: _rabbitMQOptions.ExchangeName,
                    routingKey: eventName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body);
            }
        }

        public void Subscribe<T, TH>(string exchange, string queue)
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {

            var eventName = typeof(T).Name;

            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            _consumerChannel = _persistentConnection.CreateModel();

            _consumerChannel.ExchangeDeclare(exchange: exchange, type: "durable");
            _consumerChannel.QueueDeclare(queue: queue, durable: true, exclusive: false, autoDelete: false, arguments: null);
            _consumerChannel.QueueBind(exchange: exchange, queue: queue, routingKey: eventName);

            var consumer = new EventingBasicConsumer(_consumerChannel);
            consumer.Received += (sender, eventArgs) =>
            {
                var routingKey = eventArgs.RoutingKey;
                var message = Encoding.UTF8.GetString(eventArgs.Body);
                Debug.WriteLine($"----- Mensagem recebida: {message}-----");
                _consumerChannel.BasicAck(eventArgs.DeliveryTag, false);
            };

            _consumerChannel.BasicConsume(queue: queue, autoAck: false, consumer: consumer);
        }

        public void Dispose()
        {
            _consumerChannel?.Dispose();
        }

    }
}
