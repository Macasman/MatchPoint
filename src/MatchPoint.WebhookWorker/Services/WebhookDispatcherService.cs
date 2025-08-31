using System.Net.Http;
using System.Text;
using System.Threading.Channels;
using MatchPoint.WebhookWorker.Data;
using MatchPoint.WebhookWorker.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MatchPoint.WebhookWorker.Services;

public sealed class WebhookDispatcherService : BackgroundService
{
    private readonly ILogger<WebhookDispatcherService> _logger;
    private readonly IWebhookQueueRepository _repo;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IOptionsMonitor<WebhookWorkerOptions> _opt;
    private readonly IConfiguration _config;

    private TimeSpan _interval;
    private string _workerName;
    private string _url;

    public WebhookDispatcherService(
        ILogger<WebhookDispatcherService> logger,
        IWebhookQueueRepository repo,
        IHttpClientFactory httpFactory,
        IOptionsMonitor<WebhookWorkerOptions> opt,
        IConfiguration config)
    {
        _logger = logger;
        _repo = repo;
        _httpFactory = httpFactory;
        _opt = opt;
        _config = config;

        var o = _opt.CurrentValue;
        _workerName = string.IsNullOrWhiteSpace(o.WorkerName) ? "PaymentsWebhook" : o.WorkerName;

        // Lê ServiceExecution{WorkerName} como TimeSpan (ex.: "00:00:30")
        var intervalStr = _config[$"ServiceExecution{_workerName}"];
        _interval = TimeSpan.TryParse(intervalStr, out var ts) ? ts : TimeSpan.FromSeconds(30);

        _url = _config["UrlToRequest"] ?? throw new InvalidOperationException("Missing UrlToRequest");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WebhookDispatcherService ({Worker}) iniciado. Intervalo={Interval}, Url={Url}",
            _workerName, _interval, _url);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Dequeue
                var batch = await _repo.DequeueBatchAsync(_opt.CurrentValue.BatchSize, stoppingToken);
                if (batch.Count == 0)
                {
                    await Task.Delay(_interval, stoppingToken);
                    continue;
                }

                // Processa em paralelo moderado
                var parallelism = Environment.ProcessorCount; // ajuste se desejar
                var channel = Channel.CreateBounded<WebhookJob>(batch.Count);
                var writer = channel.Writer;
                foreach (var job in batch) await writer.WriteAsync(job, stoppingToken);
                writer.Complete();

                var workers = Enumerable.Range(0, parallelism).Select(_ => Task.Run(() => WorkerLoop(channel.Reader, stoppingToken)));
                await Task.WhenAll(workers);
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha no ciclo do worker {WorkerName}", _workerName);
                // Pequeno respiro para evitar loop quente em caso de erro global
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            // Espera até a próxima varredura
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task WorkerLoop(ChannelReader<WebhookJob> reader, CancellationToken ct)
    {
        while (await reader.WaitToReadAsync(ct))
        {
            while (reader.TryRead(out var job))
            {
                await ProcessJob(job, ct);
            }
        }
    }

    private async Task ProcessJob(WebhookJob job, CancellationToken ct)
    {
        var http = _httpFactory.CreateClient(nameof(WebhookDispatcherService));
        http.Timeout = TimeSpan.FromSeconds(_opt.CurrentValue.HttpTimeoutSeconds);

        try
        {
            using var content = new StringContent(job.Payload, Encoding.UTF8, "application/json");
            using var resp = await http.PostAsync(_url, content, ct);

            if (resp.IsSuccessStatusCode)
            {
                await _repo.AckSuccessAsync(job.Id, ct);
                _logger.LogInformation("Webhook {JobId} enviado com sucesso. ({Status})", job.Id, resp.StatusCode);
            }
            else
            {
                var text = await resp.Content.ReadAsStringAsync(ct);
                await _repo.AckFailureAsync(job.Id, job.Attempts, _opt.CurrentValue.MaxAttempts, _opt.CurrentValue.BackoffSecondsBase,
                    $"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}: {Truncate(text, 256)}", ct);
                _logger.LogWarning("Webhook {JobId} falhou: {Status} {Reason}", job.Id, resp.StatusCode, resp.ReasonPhrase);
            }
        }
        catch (Exception ex)
        {
            await _repo.AckFailureAsync(job.Id, job.Attempts, _opt.CurrentValue.MaxAttempts, _opt.CurrentValue.BackoffSecondsBase,
                ex.Message, ct);
            _logger.LogError(ex, "Erro ao enviar webhook {JobId}", job.Id);
        }
    }

    private static string Truncate(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s[..max]);
}
