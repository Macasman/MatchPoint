namespace MatchPoint.Application.Contracts.Requests
{
    public sealed record CreateResourceRequest(
        string Name,
        string? Location,
        int PricePerHourCents,
        string Currency = "BRL",
        bool IsActive = true
    );
}
