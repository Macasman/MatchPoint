using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MatchPoint.API.Middlewares;

public sealed class CorrelationAndBufferingMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationAndBufferingMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        // CorrelationId (usa o do cliente se veio, senão gera)
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var cid)
            ? cid.ToString()
            : Guid.NewGuid().ToString("N");

        context.Response.Headers[HeaderName] = correlationId;
        context.Items[HeaderName] = correlationId;

        // Habilita relitura do body
        context.Request.EnableBuffering();

        await _next(context);
    }
}
