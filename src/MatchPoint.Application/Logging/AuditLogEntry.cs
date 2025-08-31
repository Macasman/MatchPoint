public sealed class AuditLogEntry
{
    public string Id { get; set; } = default!;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    // Request
    public string Method { get; set; } = default!;
    public string Scheme { get; set; } = default!;
    public string Host { get; set; } = default!;
    public string Path { get; set; } = default!;
    public string? QueryString { get; set; }
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    public string? RequestBody { get; set; }

    // Response
    public int StatusCode { get; set; }
    public IDictionary<string, string> ResponseHeaders { get; set; } = new Dictionary<string, string>();
    public string? ResponseBody { get; set; }

    // Contexto
    public string CorrelationId { get; set; } = default!;
    public string? JwtSubject { get; set; }
    public string? ClientIp { get; set; }
    public long ElapsedMs { get; set; }
}
