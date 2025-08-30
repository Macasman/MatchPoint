using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MatchPoint.Application.Contracts.Responses;
using MatchPoint.Application.Interfaces;
using static MatchPoint.Domain.Enums.Enums; // <- onde está o ReservationStatus

public sealed record ListReservationsByUserQuery(
    long UserId,
    DateTime? From,
    DateTime? To,
    ReservationStatus? Status,   // 👈 enum aqui
    int Page = 1,
    int PageSize = 20
) : IRequest<(IReadOnlyList<ReservationDto> Items, int Total)>;

public sealed class ListReservationsByUserQueryHandler
    : IRequestHandler<ListReservationsByUserQuery, (IReadOnlyList<ReservationDto>, int)>
{
    private readonly IReservationRepository _repo;
    public ListReservationsByUserQueryHandler(IReservationRepository repo) => _repo = repo;

    public async Task<(IReadOnlyList<ReservationDto>, int)> Handle(
        ListReservationsByUserQuery q, CancellationToken ct)
    {
        // 👇 converte para byte? apenas para o repositório (SQL usa tinyint)
        byte? statusByte = q.Status.HasValue ? (byte?)(byte)q.Status.Value : null;

        var (items, total) = await _repo.ListByUserAsync(
            q.UserId, q.From, q.To, statusByte, q.Page, q.PageSize, ct);

        // Se seu DTO expõe byte Status
        var dtos = items.Select(e => new ReservationDto(
            e.ReservationId, e.UserId, e.ResourceId, e.StartTime, e.EndTime,
            e.PriceCents, e.Currency, (byte)e.Status, // enum -> byte
            e.Notes, e.CreationDate, e.UpdateDate
        )).ToList();

        // 👉 Se você já ajustou o ReservationDto para expor ReservationStatus,
        // basta usar e.Status direto sem cast:
        // var dtos = items.Select(e => new ReservationDto(
        //     e.ReservationId, e.UserId, e.ResourceId, e.StartTime, e.EndTime,
        //     e.PriceCents, e.Currency, e.Status,
        //     e.Notes, e.CreationDate, e.UpdateDate
        // )).ToList();

        return (dtos, total);
    }
}
