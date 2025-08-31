using System;
using System.Threading;
using System.Threading.Tasks;

namespace MatchPoint.Application.Interfaces
{
    public interface IWebhookQueueWriter
    {
        Task<long> EnqueuePaymentEventAsync(
            long paymentIntentId,
            string @event,                // "payment.captured" | "payment.failed" | "payment.canceled"
            string? providerRef = null,
            DateTime? scheduleUtc = null, // vira NextAttemptUtc
            CancellationToken ct = default);
    }
}
