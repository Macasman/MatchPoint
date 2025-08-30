namespace MatchPoint.Domain.Entities;

public class User
{
    public long UserId { get; set; }
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Phone { get; set; }
    public string? DocumentId { get; set; } // CPF/CNPJ sem máscara
    public DateTime? BirthDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreationDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? PasswordHash { get; set; }
}
