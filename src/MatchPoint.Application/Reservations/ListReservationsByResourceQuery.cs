using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MatchPoint.Application.Contracts.Responses;
using MatchPoint.Application.Interfaces;
using static MatchPoint.Domain.Enums.Enums; // <- onde está o enum ReservationStatus

public sealed record ListReservationsByResourceQuery(
    long ResourceId,
    DateTime? From,
    DateTime? To,
    ReservationStatus? Status,   // <- enum aqui
    int Page = 1,
    int PageSize = 20
) : IRequest<(IReadOnlyList<ReservationDto> Items, int Total)>;

public sealed class ListReservationsByResourceQueryHandler
    : IRequestHandler<ListReservationsByResourceQuery, (IReadOnlyList<ReservationDto>, int)>
{
    private readonly IReservationRepository _repo;
    public ListReservationsByResourceQueryHandler(IReservationRepository repo) => _repo = repo;

    public async Task<(IReadOnlyList<ReservationDto>, int)> Handle(
        ListReservationsByResourceQuery q, CancellationToken ct)
    {
        // converte enum? -> byte? só para o repositório (SQL usa TINYINT)
        byte? statusByte = q.Status.HasValue ? (byte?)(byte)q.Status.Value : null;

        var (items, total) = await _repo.ListByResourceAsync(
            q.ResourceId, q.From, q.To, statusByte, q.Page, q.PageSize, ct);

        // Se seu DTO ainda expõe byte Status:
        var dtos = items.Select(e => new ReservationDto(
            e.ReservationId, e.UserId, e.ResourceId, e.StartTime, e.EndTime,
            e.PriceCents, e.Currency, (byte)e.Status,  // <- enum -> byte para o DTO
            e.Notes, e.CreationDate, e.UpdateDate
        )).ToList();

        // Caso você tenha trocado o DTO para expor ReservationStatus, use direto:
        // var dtos = items.Select(e => new ReservationDto(
        //     e.ReservationId, e.UserId, e.ResourceId, e.StartTime, e.EndTime,
        //     e.PriceCents, e.Currency, e.Status,
        //     e.Notes, e.CreationDate, e.UpdateDate
        // )).ToList();

        return (dtos, total);
    }
}
