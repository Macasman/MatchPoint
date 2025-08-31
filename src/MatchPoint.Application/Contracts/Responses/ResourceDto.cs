using System;

namespace MatchPoint.Application.Contracts.Responses
{
    public sealed record ResourceDto(
        long ResourceId,
        string Name,
        string? Location,
        int PricePerHourCents,
        string Currency,
        bool IsActive,
        DateTime CreationDate,
        DateTime? UpdateDate
    );
}
