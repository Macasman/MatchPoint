namespace MatchPoint.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(string routingKey, T payload, CancellationToken ct);
}
