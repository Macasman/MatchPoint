namespace MatchPoint.Domain.Entities;

public class KycVerification
{
    public long KycId { get; set; }
    public long UserId { get; set; }
    public byte Status { get; set; } = 0; // 0=Pendente, 1=Aprovado, 2=Reprovado, 3=Analise
    public string? Provider { get; set; } // ex.: "Simulado" / "DocOCR"
    public decimal? Score { get; set; }   // 0..100 (pode ser null)
    public string? Notes { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime? UpdateDate { get; set; }
}
