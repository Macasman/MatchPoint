using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MatchPoint.Application.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MatchPoint.API.Filters;

public sealed class AuditLogActionFilter : IAsyncActionFilter
{
    private static readonly string CorrelationKey = "X-Correlation-Id";
    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization", "Cookie", "Set-Cookie"
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var sw = Stopwatch.StartNew();

        // --- Request ---
        string? requestBody = null;
        if ((http.Request.ContentLength ?? 0) > 0 && http.Request.Body.CanRead)
        {
            http.Request.Body.Position = 0;
            using var reader = new StreamReader(http.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            http.Request.Body.Position = 0;
        }

        // Headers sem dados sensíveis
        var safeReqHeaders = new Dictionary<string, string>();
        foreach (var h in http.Request.Headers)
        {
            if (SensitiveHeaders.Contains(h.Key)) continue;
            safeReqHeaders[h.Key] = string.Join(",", h.Value);
        }

        // Executa action
        var executed = await next();
        sw.Stop();

        // --- Response ---
        int statusCode = http.Response.StatusCode;

        string? responseBody = null;
        if (executed.Result is ObjectResult obj)
        {
            responseBody = SafeSerialize(obj.Value);
        }

        var safeRespHeaders = new Dictionary<string, string>();
        foreach (var h in http.Response.Headers)
        {
            if (SensitiveHeaders.Contains(h.Key)) continue;
            safeRespHeaders[h.Key] = string.Join(",", h.Value);
        }

        // JWT subject (quando autenticado)
        var subject = http.User?.FindFirst("sub")?.Value ?? http.User?.Identity?.Name;

        // CorrelationId
        var correlationId = http.Items.TryGetValue(CorrelationKey, out var cidObj) ? cidObj?.ToString() : null;
        correlationId ??= http.Response.Headers.TryGetValue(CorrelationKey, out var cid) ? cid.ToString() : Guid.NewGuid().ToString("N");

        // Monta log
        var entry = new AuditLogEntry
        {
            TimestampUtc = DateTime.UtcNow,
            Method = http.Request.Method,
            Scheme = http.Request.Scheme,
            Host = http.Request.Host.Value,
            Path = http.Request.Path.Value ?? string.Empty,
            QueryString = http.Request.QueryString.HasValue ? http.Request.QueryString.Value : null,
            Headers = safeReqHeaders,
            RequestBody = requestBody,
            StatusCode = statusCode,
            ResponseHeaders = safeRespHeaders,
            ResponseBody = responseBody,
            CorrelationId = correlationId,
            JwtSubject = subject,
            ClientIp = http.Connection.RemoteIpAddress?.ToString(),
            ElapsedMs = sw.ElapsedMilliseconds
        };

        // Persiste (fire-and-forget para não impactar latência)
        var logger = http.RequestServices.GetRequiredService<IAuditLogService>();
        await logger.WriteAsync(entry); // aguarda e propaga erro, se houver
    }

    private static string? SafeSerialize(object? value)
    {
        try
        {
            return JsonSerializer.Serialize(value, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
        }
        catch { return null; }
    }
}
