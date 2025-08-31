using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchPoint.WebhookWorker.Models
{
    public sealed class WebhookWorkerOptions
    {
        public string WorkerName { get; set; } = "PaymentsWebhook";
        public int BatchSize { get; set; } = 200;
        public int MaxAttempts { get; set; } = 6;
        public int HttpTimeoutSeconds { get; set; } = 10;
        public int BackoffSecondsBase { get; set; } = 30; // backoff exponencial
    }
}
