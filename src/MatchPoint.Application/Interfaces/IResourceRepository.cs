public interface IResourceRepository
{
    Task<long> CreateAsync(Resource r, CancellationToken ct);
    Task<Resource?> GetByIdAsync(long id, CancellationToken ct);
    Task<IReadOnlyList<Resource>> ListAsync(bool? onlyActive, CancellationToken ct);
    Task<bool> UpdateAsync(Resource r, CancellationToken ct); // nome, preço, ativo...
}
