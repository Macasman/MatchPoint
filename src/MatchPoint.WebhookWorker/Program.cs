using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MatchPoint.WebhookWorker.Data;
using MatchPoint.WebhookWorker.Models;
using MatchPoint.WebhookWorker.Services;

var builder = Host.CreateApplicationBuilder(args);

// Options
builder.Services.Configure<WebhookWorkerOptions>(builder.Configuration.GetSection("WebhookWorker"));

// Infra
builder.Services.AddSingleton<SqlDbContext>();
builder.Services.AddSingleton<IWebhookQueueRepository, WebhookQueueRepository>(); // <- Singleton para evitar o erro de lifetime

// HttpClient (IHttpClientFactory)
builder.Services.AddHttpClient(nameof(WebhookDispatcherService));

// BackgroundService
builder.Services.AddHostedService<WebhookDispatcherService>();

await builder.Build().RunAsync();
