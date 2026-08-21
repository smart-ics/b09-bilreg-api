using Bilreg.Domain.ApotekContext.ResepKerjaFeature;

namespace Bilreg.Application.ApotekContext.ResepKerjaFeature;

public record PrescriptionContractItem(
    int SourceItemNo,
    string BrgId,
    string BrgName,
    string SatuanId,
    string SatuanName,
    decimal Qty,
    int Iter,
    string Signa,
    string Instruction,
    string Note,
    bool IsRacik);

public record PrescriptionContractComponent(
    int SourceItemNo,
    int ComponentNo,
    string BrgId,
    string BrgName,
    string SatuanId,
    decimal Qty);

public record PrescriptionContract(
    ResepKerjaSourceKindEnum SourceKind,
    string SourceResepId,
    string RegId,
    string PasienId,
    string PasienName,
    string DokterId,
    string DokterName,
    string LayananId,
    int Urgenitas,
    int IterEntitled,
    IReadOnlyList<PrescriptionContractItem> Items,
    IReadOnlyList<PrescriptionContractComponent> Components);

public interface IPrescriptionContractPort
{
    PrescriptionContract Load(ResepKerjaSourceKindEnum sourceKind, string sourceResepId);
}
