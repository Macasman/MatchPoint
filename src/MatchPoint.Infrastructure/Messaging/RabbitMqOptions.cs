namespace MatchPoint.Infrastructure.Messaging;

public class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "matchpoint.reservations";
    public string ExchangeType { get; set; } = "topic";
    public bool Durable { get; set; } = true;
}
