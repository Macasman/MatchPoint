using System.Data.SqlClient;
using MatchPoint.Application.Interfaces;
using MatchPoint.Domain.Entities;
using MatchPoint.Infrastructure.Persistence;

namespace MatchPoint.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly SqlDbContext _db;
    public UserRepository(SqlDbContext db) => _db = db;

    public async Task<long> CreateAsync(User e, CancellationToken ct)
    {
        const string sql = @"
                INSERT INTO core.Users (Name, Email, Phone, DocumentId, BirthDate, IsActive, CreationDate, PasswordHash)
                OUTPUT INSERTED.UserId
                VALUES (@Name, @Email, @Phone, @DocumentId, @BirthDate, @IsActive, @CreationDate, @PasswordHash)";

        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("@Name", e.Name);
        cmd.Parameters.AddWithValue("@Email", e.Email);
        cmd.Parameters.AddWithValue("@Phone", (object?)e.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DocumentId", (object?)e.DocumentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@BirthDate", (object?)e.BirthDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PasswordHash", (object?)e.PasswordHash ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsActive", (object?)e.IsActive ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreationDate", (object?)e.CreationDate ?? DBNull.Value);

        var id = (long)await cmd.ExecuteScalarAsync(ct);
        return id;
    }

    public async Task<User?> GetByIdAsync(long id, CancellationToken ct)
    {
        const string sql = @"
SELECT UserId, Name, Email, Phone, DocumentId, BirthDate, IsActive, CreationDate, UpdateDate
FROM core.Users WITH (NOLOCK)
WHERE UserId = @Id;";

        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);

        using var rd = await cmd.ExecuteReaderAsync(ct);
        if (!await rd.ReadAsync(ct)) return null;

        return new User
        {
            UserId = rd.GetInt64(0),
            Name = rd.GetString(1),
            Email = rd.GetString(2),
            Phone = rd.IsDBNull(3) ? null : rd.GetString(3),
            DocumentId = rd.IsDBNull(4) ? null : rd.GetString(4),
            BirthDate = rd.IsDBNull(5) ? null : rd.GetDateTime(5),
            IsActive = rd.GetBoolean(6),
            CreationDate = rd.GetDateTime(7),
            UpdateDate = rd.IsDBNull(8) ? null : rd.GetDateTime(8)
        };
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct)
    {
        const string sql = @"SELECT 1 FROM core.Users WITH (NOLOCK) WHERE Email = @Email;";
        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Email", email);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is not null;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        const string sql = @"SELECT TOP 1 UserId, Name, Email, Phone, DocumentId, BirthDate, IsActive, PasswordHash
                         FROM core.Users WHERE Email = @Email";

        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Email", email);

        using var rd = await cmd.ExecuteReaderAsync(ct);
        if (!await rd.ReadAsync(ct)) return null;

        return new User
        {
            UserId = rd.GetInt64(0),
            Name = rd.GetString(1),
            Email = rd.GetString(2),
            Phone = rd.IsDBNull(3) ? null : rd.GetString(3),
            DocumentId = rd.IsDBNull(4) ? null : rd.GetString(4),
            BirthDate = rd.IsDBNull(5) ? null : rd.GetDateTime(5),
            IsActive = rd.GetBoolean(6),
            PasswordHash = rd.IsDBNull(7) ? null : rd.GetString(7)
        };
    }

}
