public sealed record ReservationDto(
    long ReservationId,
    long UserId,
    long ResourceId,
    DateTime StartTime,
    DateTime EndTime,
    int PriceCents,
    string Currency,
    byte Status,          // ou seu enum
    string? Notes,
    DateTime CreationDate,
    DateTime? UpdateDate
);
