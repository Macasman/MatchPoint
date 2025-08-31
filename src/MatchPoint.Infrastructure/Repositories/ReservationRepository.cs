using System.Data;
using System.Data.SqlClient;
using MatchPoint.Application.Interfaces;
using MatchPoint.Domain.Entities;
using MatchPoint.Infrastructure.Persistence;
using static MatchPoint.Domain.Enums.Enums;

namespace MatchPoint.Infrastructure.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly SqlDbContext _db;
    public ReservationRepository(SqlDbContext db) => _db = db;

    public async Task<long> CreateAsync(Reservation e, CancellationToken ct)
    {
        const string sql = @"
        INSERT INTO booking.Reservations
        (UserId, ResourceId, StartTime, EndTime, Status, PriceCents, Currency, Notes)
        OUTPUT INSERTED.ReservationId
        VALUES (@UserId, @ResourceId, @StartTime, @EndTime, @Status, @PriceCents, @Currency, @Notes);";

        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.BigInt) { Value = e.UserId });
        cmd.Parameters.Add(new SqlParameter("@ResourceId", SqlDbType.BigInt) { Value = e.ResourceId });
        cmd.Parameters.Add(new SqlParameter("@StartTime", SqlDbType.DateTime2) { Value = e.StartTime });
        cmd.Parameters.Add(new SqlParameter("@EndTime", SqlDbType.DateTime2) { Value = e.EndTime });
        // 👇 enum -> tinyint
        cmd.Parameters.Add(new SqlParameter("@Status", SqlDbType.TinyInt) { Value = (byte)e.Status });
        cmd.Parameters.Add(new SqlParameter("@PriceCents", SqlDbType.Int) { Value = e.PriceCents });
        cmd.Parameters.Add(new SqlParameter("@Currency", SqlDbType.Char, 3) { Value = e.Currency });
        cmd.Parameters.Add(new SqlParameter("@Notes", SqlDbType.NVarChar, -1) { Value = (object?)e.Notes ?? DBNull.Value });

        var id = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(id);
    }


    public async Task<Reservation?> GetByIdAsync(long id, CancellationToken ct)
    {
        const string sql = @"
        SELECT ReservationId, UserId, ResourceId, StartTime, EndTime, Status, PriceCents, Currency, Notes, CreationDate, UpdateDate
        FROM booking.Reservations WITH (NOLOCK)
        WHERE ReservationId = @Id;";

        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.BigInt) { Value = id });

        using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, ct);
        if (!await rd.ReadAsync(ct)) return null;

        int iStatus = rd.GetOrdinal("Status");

        return new Reservation
        {
            ReservationId = rd.GetInt64(0),
            UserId = rd.GetInt64(1),
            ResourceId = rd.GetInt64(2),
            StartTime = rd.GetDateTime(3),
            EndTime = rd.GetDateTime(4),
            Status = (ReservationStatus)rd.GetByte(iStatus),  // 👈 cast para enum
            PriceCents = rd.GetInt32(6),
            Currency = rd.GetString(7),
            Notes = rd.IsDBNull(8) ? null : rd.GetString(8),
            CreationDate = rd.GetDateTime(9),
            UpdateDate = rd.IsDBNull(10) ? (DateTime?)null : rd.GetDateTime(10)
        };
    }


    static string BuildWhere(string baseWhere, DateTime? from, DateTime? to, byte? status)
    {
        var where = baseWhere;
        if (from.HasValue) where += " AND StartTime >= @From";
        if (to.HasValue) where += " AND StartTime <  @To";
        if (status.HasValue) where += " AND Status = @Status";
        return where;
    }

    static void BindCommon(SqlCommand cmd, DateTime? from, DateTime? to, byte? status)
    {
        if (from.HasValue) cmd.Parameters.Add(new SqlParameter("@From", SqlDbType.DateTime2) { Value = from.Value });
        if (to.HasValue) cmd.Parameters.Add(new SqlParameter("@To", SqlDbType.DateTime2) { Value = to.Value });
        if (status.HasValue) cmd.Parameters.Add(new SqlParameter("@Status", SqlDbType.TinyInt) { Value = status.Value });
    }

    public async Task<(IReadOnlyList<Reservation> Items, int Total)> ListByUserAsync(
        long userId, DateTime? from, DateTime? to, byte? status, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1) page = 1;
        if (pageSize <= 0) pageSize = 20;

        var where = BuildWhere("WHERE UserId = @UserId", from, to, status);

        var sqlCount = $"SELECT COUNT(1) FROM booking.Reservations WITH (NOLOCK) {where};";
        var sqlItems = $@"
            SELECT ReservationId, UserId, ResourceId, StartTime, EndTime, PriceCents, Currency, Status, Notes, CreationDate, UpdateDate
            FROM booking.Reservations WITH (NOLOCK)
            {where}
            ORDER BY StartTime DESC
            OFFSET {(page - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY;";

        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        // total
        int total;
        using (var cmd = new SqlCommand(sqlCount, conn))
        {
            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.BigInt) { Value = userId });
            BindCommon(cmd, from, to, status);
            total = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        }

        // items
        var list = new List<Reservation>();
        using (var cmd = new SqlCommand(sqlItems, conn))
        {
            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.BigInt) { Value = userId });
            BindCommon(cmd, from, to, status);

            using var rd = await cmd.ExecuteReaderAsync(ct);
            int i(string n) => rd.GetOrdinal(n);
            while (await rd.ReadAsync(ct))
            {
                list.Add(new Reservation
                {
                    ReservationId = rd.GetInt64(i("ReservationId")),
                    UserId = rd.GetInt64(i("UserId")),
                    ResourceId = rd.GetInt64(i("ResourceId")),
                    StartTime = rd.GetDateTime(i("StartTime")),
                    EndTime = rd.GetDateTime(i("EndTime")),
                    PriceCents = rd.GetInt32(i("PriceCents")),
                    Currency = rd.GetString(i("Currency")),
                    Status = (ReservationStatus)rd.GetByte(i("Status")),   // 👈
                    Notes = rd.IsDBNull(i("Notes")) ? null : rd.GetString(i("Notes")),
                    CreationDate = rd.GetDateTime(i("CreationDate")),
                    UpdateDate = rd.IsDBNull(i("UpdateDate")) ? (DateTime?)null : rd.GetDateTime(i("UpdateDate"))
                });
            }
        }
        return (list, total);
    }

    public async Task<(IReadOnlyList<Reservation> Items, int Total)> ListByResourceAsync(
        long resourceId, DateTime? from, DateTime? to, byte? status, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1) page = 1;
        if (pageSize <= 0) pageSize = 20;

        var where = BuildWhere("WHERE ResourceId = @ResourceId", from, to, status);

        var sqlCount = $"SELECT COUNT(1) FROM booking.Reservations WITH (NOLOCK) {where};";
        var sqlItems = $@"
            SELECT ReservationId, UserId, ResourceId, StartTime, EndTime, PriceCents, Currency, Status, Notes, CreationDate, UpdateDate
            FROM booking.Reservations WITH (NOLOCK)
            {where}
            ORDER BY StartTime DESC
            OFFSET {(page - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY;";

        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        int total;
        using (var cmd = new SqlCommand(sqlCount, conn))
        {
            cmd.Parameters.Add(new SqlParameter("@ResourceId", SqlDbType.BigInt) { Value = resourceId });
            BindCommon(cmd, from, to, status);
            total = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        }

        var list = new List<Reservation>();
        using (var cmd = new SqlCommand(sqlItems, conn))
        {
            cmd.Parameters.Add(new SqlParameter("@ResourceId", SqlDbType.BigInt) { Value = resourceId });
            BindCommon(cmd, from, to, status);

            using var rd = await cmd.ExecuteReaderAsync(ct);
            int i(string n) => rd.GetOrdinal(n);
            while (await rd.ReadAsync(ct))
            {
                list.Add(new Reservation
                {
                    ReservationId = rd.GetInt64(i("ReservationId")),
                    UserId = rd.GetInt64(i("UserId")),
                    ResourceId = rd.GetInt64(i("ResourceId")),
                    StartTime = rd.GetDateTime(i("StartTime")),
                    EndTime = rd.GetDateTime(i("EndTime")),
                    PriceCents = rd.GetInt32(i("PriceCents")),
                    Currency = rd.GetString(i("Currency")),
                    Status = (ReservationStatus)rd.GetByte(i("Status")),   // 👈
                    Notes = rd.IsDBNull(i("Notes")) ? null : rd.GetString(i("Notes")),
                    CreationDate = rd.GetDateTime(i("CreationDate")),
                    UpdateDate = rd.IsDBNull(i("UpdateDate")) ? (DateTime?)null : rd.GetDateTime(i("UpdateDate"))
                });
            }
        }
        return (list, total);
    }

    public async Task<bool> CancelAsync(long reservationId, CancellationToken ct)
    {
        const string sql = @"
            UPDATE booking.Reservations
               SET Status = @Canceled,
                   UpdateDate = SYSUTCDATETIME()
             WHERE ReservationId = @Id
               AND Status = @Scheduled;"; 

    using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.BigInt) { Value = reservationId });
        cmd.Parameters.Add(new SqlParameter("@Canceled", SqlDbType.TinyInt) { Value = (byte)ReservationStatus.CanceledByUser });
        cmd.Parameters.Add(new SqlParameter("@Scheduled", SqlDbType.TinyInt) { Value = (byte)ReservationStatus.Scheduled });

        var rows = await cmd.ExecuteNonQueryAsync(ct);
        return rows > 0; // true = cancelou, false = não estava Agendada ou não existe
    }

    public async Task<(bool Created, long Id)> CreateIfNoOverlapAsync(Reservation e, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        // 1) Checagem de sobreposição com locks (UPDLOCK + HOLDLOCK) dentro de SERIALIZABLE
        const string sqlCheck = @"
            SELECT TOP(1) 1
            FROM booking.Reservations WITH (UPDLOCK, HOLDLOCK)
            WHERE ResourceId = @ResourceId
              AND Status IN (1,2) -- Agendada/Concluida bloqueiam; ajuste se quiser só Agendada
              AND StartTime < @EndTime
              AND EndTime   > @StartTime;";

        using (var check = new SqlCommand(sqlCheck, conn, tx))
        {
            check.Parameters.Add(new SqlParameter("@ResourceId", SqlDbType.BigInt) { Value = e.ResourceId });
            check.Parameters.Add(new SqlParameter("@StartTime", SqlDbType.DateTime2) { Value = e.StartTime });
            check.Parameters.Add(new SqlParameter("@EndTime", SqlDbType.DateTime2) { Value = e.EndTime });
            var hasOverlap = (await check.ExecuteScalarAsync(ct)) != null;
            if (hasOverlap)
            {
                tx.Rollback();
                return (false, 0L);
            }
        }

        // 2) Insert
        const string sqlInsert = @"
            INSERT INTO booking.Reservations
            (UserId, ResourceId, StartTime, EndTime, Status, PriceCents, Currency, Notes)
            OUTPUT INSERTED.ReservationId
            VALUES (@UserId, @ResourceId, @StartTime, @EndTime, @Status, @PriceCents, @Currency, @Notes);";

        using (var cmd = new SqlCommand(sqlInsert, conn, tx))
        {
            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.BigInt) { Value = e.UserId });
            cmd.Parameters.Add(new SqlParameter("@ResourceId", SqlDbType.BigInt) { Value = e.ResourceId });
            cmd.Parameters.Add(new SqlParameter("@StartTime", SqlDbType.DateTime2) { Value = e.StartTime });
            cmd.Parameters.Add(new SqlParameter("@EndTime", SqlDbType.DateTime2) { Value = e.EndTime });
            cmd.Parameters.Add(new SqlParameter("@Status", SqlDbType.TinyInt) { Value = (byte)e.Status });
            cmd.Parameters.Add(new SqlParameter("@PriceCents", SqlDbType.Int) { Value = e.PriceCents });
            cmd.Parameters.Add(new SqlParameter("@Currency", SqlDbType.Char, 3) { Value = e.Currency });
            cmd.Parameters.Add(new SqlParameter("@Notes", SqlDbType.NVarChar, -1) { Value = (object?)e.Notes ?? DBNull.Value });

            var id = Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
            tx.Commit();
            return (true, id);
        }
    }
}
