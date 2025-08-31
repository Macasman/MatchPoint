using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchPoint.WebhookWorker.Models
{
    public sealed class WebhookJob
    {
        public long Id { get; init; }
        public string AggregateType { get; init; } = "";
        public long AggregateId { get; init; }
        public string Payload { get; init; } = "{}";
        public int Attempts { get; init; }
    }
}
