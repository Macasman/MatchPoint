// MatchPoint.Application/Contracts/Requests/UpdateResourceRequest.cs
namespace MatchPoint.Application.Contracts.Requests
{
    public sealed record UpdateResourceRequest(
        string Name,
        string? Location,
        int PricePerHourCents,
        bool IsActive
    );
}
