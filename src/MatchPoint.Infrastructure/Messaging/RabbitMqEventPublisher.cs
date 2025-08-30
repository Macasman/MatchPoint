using System.Text;
using System.Text.Json;
using MatchPoint.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MatchPoint.Infrastructure.Messaging;

public class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly RabbitMqOptions _opt;
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqEventPublisher(IOptions<RabbitMqOptions> opt, ILogger<RabbitMqEventPublisher> logger)
    {
        _opt = opt.Value;
        _logger = logger;
        EnsureConnection();
        EnsureExchange();
    }

    private void EnsureConnection()
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
            return;

        _logger.LogInformation("🔌 Connecting to RabbitMQ at {Host}:{Port} with user={User}",
            _opt.Host, _opt.Port, _opt.User);

        var factory = new ConnectionFactory
        {
            HostName = _opt.Host,
            Port = _opt.Port,
            UserName = _opt.User,
            Password = _opt.Password
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _logger.LogInformation("✅ Connection established and channel opened.");
    }

    private void EnsureExchange()
    {
        _channel!.ExchangeDeclare(exchange: _opt.Exchange, type: _opt.ExchangeType, durable: _opt.Durable);
        _logger.LogInformation("📦 Exchange declared: {Exchange} (type={Type}, durable={Durable})",
            _opt.Exchange, _opt.ExchangeType, _opt.Durable);
    }

    public Task PublishAsync<T>(string routingKey, T payload, CancellationToken ct)
    {
        EnsureConnection();

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var body = Encoding.UTF8.GetBytes(json);

        var props = _channel!.CreateBasicProperties();
        props.ContentType = "application/json";
        props.DeliveryMode = 2;

        _channel.BasicPublish(exchange: _opt.Exchange, routingKey: routingKey, basicProperties: props, body: body);

        _logger.LogInformation("📤 Published event to RabbitMQ. Exchange={Exchange}, RoutingKey={RoutingKey}, Payload={Payload}",
            _opt.Exchange, routingKey, json);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try { _channel?.Close(); } catch { }
        try { _channel?.Dispose(); } catch { }
        try { _connection?.Close(); } catch { }
        try { _connection?.Dispose(); } catch { }
    }
}
