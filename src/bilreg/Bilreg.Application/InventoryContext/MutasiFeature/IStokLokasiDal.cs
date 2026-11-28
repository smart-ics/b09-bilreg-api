namespace Bilreg.Application.InventoryContext.MutasiFeature;

public interface IStokLokasiDal
{
    IEnumerable<IStokLokasiCandidate> ListAllocationCandidates(string brgId, string layananId);
    IEnumerable<MutasiBrgStokLokasiView> ListStokLokasi(string layananId);
}

public interface IStokLokasiCandidate
{
    decimal QtySisa { get; }
}

public record StokLokasiCandidate(decimal QtySisa) : IStokLokasiCandidate;

public record MutasiBrgStokLokasiView(
    string BrgId,
    string BrgName,
    decimal TotalStok,
    string SatuanId,
    string SatuanName,
    string GroupRekDkId,
    decimal HppDefault,
    IReadOnlyList<MutasiBrgStokBatchView> Batches
);

public record MutasiBrgStokBatchView(
    string NoBatch,
    DateTime TglEd,
    decimal QtySisa,
    decimal Hpp
);
