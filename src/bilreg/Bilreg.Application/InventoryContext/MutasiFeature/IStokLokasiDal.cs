namespace Bilreg.Application.InventoryContext.MutasiFeature;

public interface IStokLokasiDal
{
    IEnumerable<IStokLokasiCandidate> ListAllocationCandidates(string brgId, string layananId);
}

public interface IStokLokasiCandidate
{
    decimal QtySisa { get; }
}

public record StokLokasiCandidate(decimal QtySisa) : IStokLokasiCandidate;
