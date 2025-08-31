using System.Collections.Generic;
using System.Threading.Tasks;
using MatchPoint.Application.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MatchPoint.Infrastructure.Logging;

public sealed class MongoAuditLogService : IAuditLogService
{
    private readonly IMongoCollection<BsonDocument> _col;
    private readonly MongoAuditLogOptions _opt;

    public MongoAuditLogService(MongoAuditLogOptions opt)
    {
        _opt = opt;
        var client = new MongoClient(opt.ConnectionString);
        var db = client.GetDatabase(opt.Database);
        _col = db.GetCollection<BsonDocument>(opt.Collection);

        // cria índices na inicialização (sync-over-async de propósito aqui)
        EnsureIndexesAsync(_col, opt).GetAwaiter().GetResult();
    }

    public async Task WriteAsync(AuditLogEntry entry)
    {
        var doc = new BsonDocument
    {
        { "TimestampUtc", entry.TimestampUtc },
        { "Method", entry.Method },
        { "Scheme", entry.Scheme },
        { "Host", entry.Host },
        { "Path", entry.Path },
        { "QueryString",  Safe(entry.QueryString) },
        { "Headers",      SafeDict(entry.Headers) },
        { "RequestBody",  Safe(entry.RequestBody) },
        { "StatusCode", entry.StatusCode },
        { "ResponseHeaders", SafeDict(entry.ResponseHeaders) },
        { "ResponseBody",    Safe(entry.ResponseBody) },
        { "CorrelationId", entry.CorrelationId },
        { "JwtSubject",   Safe(entry.JwtSubject) },
        { "ClientIp",     Safe(entry.ClientIp) },
        { "ElapsedMs",    entry.ElapsedMs }
    };


        await _col.InsertOneAsync(doc);

    }

    // ---------- Helpers ----------

    private static BsonValue Safe(string? value)
        => value is null ? BsonNull.Value : new BsonString(value);

    private static BsonDocument SafeDict(IDictionary<string, string>? dict)
        => dict is null ? new BsonDocument() : dict.ToBsonDocument();

    private static async Task EnsureIndexesAsync(IMongoCollection<BsonDocument> col, MongoAuditLogOptions opt)
    {
        var db = col.Database;
        var idxCur = await col.Indexes.ListAsync();
        var existing = await idxCur.ToListAsync();

        bool HasIndexNamed(string name) =>
            existing.Any(i => i.TryGetValue("name", out var n) && n.IsString && n.AsString == name);

        BsonDocument? FindTimestampIdx()
        {
            return existing.FirstOrDefault(i =>
                i.TryGetValue("key", out var keyDoc) &&
                keyDoc is BsonDocument kd &&
                kd.ElementCount == 1 &&
                kd.TryGetValue("TimestampUtc", out var v) &&
                v.IsInt32 && v.AsInt32 == 1
            );
        }

        var tsIdx = FindTimestampIdx();

        // 1) Garantir TTL em TimestampUtc
        if (opt.TtlDays > 0)
        {
            var seconds = (int)TimeSpan.FromDays(opt.TtlDays).TotalSeconds;

            if (tsIdx is not null)
            {
                // Converte o índice existente (qualquer nome) para TTL
                var name = tsIdx.GetValue("name", "TimestampUtc_1").AsString;
                try
                {
                    var cmd = new BsonDocument
                {
                    { "collMod", col.CollectionNamespace.CollectionName },
                    { "index", new BsonDocument {
                        { "name", name },
                        { "expireAfterSeconds", seconds }
                    }}
                };
                    await db.RunCommandAsync<BsonDocument>(cmd);
                }
                catch
                {
                    // Se collMod não for possível (ex.: Atlas/permissions), faz fallback:
                    // Dropa e recria com TTL
                    try { await col.Indexes.DropOneAsync(name); } catch { /* ignore */ }
                    var model = new CreateIndexModel<BsonDocument>(
                        Builders<BsonDocument>.IndexKeys.Ascending("TimestampUtc"),
                        new CreateIndexOptions { Name = "TimestampUtc_1", ExpireAfter = TimeSpan.FromSeconds(seconds) });
                    try { await col.Indexes.CreateOneAsync(model); } catch { /* se criar em paralelo, ignore */ }
                }
            }
            else
            {
                // Cria índice já com TTL (padroniza o nome para evitar duplicidade)
                var model = new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("TimestampUtc"),
                    new CreateIndexOptions { Name = "TimestampUtc_1", ExpireAfter = TimeSpan.FromSeconds(seconds) });

                try { await col.Indexes.CreateOneAsync(model); } catch { /* se outro processo criou, ignore */ }
            }
        }
        else
        {
            // Sem TTL: garante o índice simples se ainda não existir
            if (tsIdx is null)
            {
                var model = new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("TimestampUtc"),
                    new CreateIndexOptions { Name = "TimestampUtc_1" });

                try { await col.Indexes.CreateOneAsync(model); } catch { /* ignore duplicates */ }
            }
        }

        // 2) Demais índices (só se faltarem)
        var toCreate = new List<CreateIndexModel<BsonDocument>>();
        void AddIfMissing(string name, IndexKeysDefinition<BsonDocument> keys)
        {
            if (!HasIndexNamed(name))
                toCreate.Add(new CreateIndexModel<BsonDocument>(keys, new CreateIndexOptions { Name = name }));
        }

        AddIfMissing("StatusCode_1", Builders<BsonDocument>.IndexKeys.Ascending("StatusCode"));
        AddIfMissing("Path_1", Builders<BsonDocument>.IndexKeys.Ascending("Path"));
        AddIfMissing("JwtSubject_1", Builders<BsonDocument>.IndexKeys.Ascending("JwtSubject"));
        AddIfMissing("CorrelationId_1", Builders<BsonDocument>.IndexKeys.Ascending("CorrelationId"));

        if (toCreate.Count > 0)
        {
            try { await col.Indexes.CreateManyAsync(toCreate); } catch { /* ignore if races */ }
        }
    }

}
