using System;

namespace MatchPoint.Domain.Entities
{
    public sealed class Resource
    {
        public long ResourceId { get; set; }
        public string Name { get; set; } = default!;
        public string? Location { get; set; }
        public int PricePerHourCents { get; set; }
        public string Currency { get; set; } = "BRL";
        public bool IsActive { get; set; } = true;
        public DateTime CreationDate { get; set; }
        public DateTime? UpdateDate { get; set; }
    }
}
