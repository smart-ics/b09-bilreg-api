using Bilreg.Domain.Shared;

namespace Bilreg.Domain.PaymentContext.KasirFeature;

public interface ILoketKasirKey
{
    string LoketId { get; }
}

public record LoketKasirKey(string LoketId) : ILoketKasirKey;

public class LoketKasirModel : ILoketKasirKey
{
    public LoketKasirModel(
        string loketId,
        string namaLoket,
        string coaId,
        string namaCoa = "",
        bool isAktif = true,
        string keterangan = "")
    {
        LoketId = loketId?.Trim() ?? string.Empty;
        NamaLoket = namaLoket?.Trim() ?? string.Empty;
        CoaId = coaId?.Trim() ?? string.Empty;
        NamaCoa = namaCoa?.Trim() ?? string.Empty;
        IsAktif = isAktif;
        Keterangan = keterangan?.Trim() ?? string.Empty;

        ValidateInvariants();
    }

    public string LoketId { get; private set; }
    public string NamaLoket { get; private set; }
    public string CoaId { get; private set; }
    public string NamaCoa { get; private set; }
    public bool IsAktif { get; private set; }
    public string Keterangan { get; private set; }

    public static LoketKasirModel Create(
        string loketId,
        string namaLoket,
        string coaId,
        string namaCoa = "",
        string keterangan = "")
    {
        return new LoketKasirModel(
            loketId: loketId,
            namaLoket: namaLoket,
            coaId: coaId,
            namaCoa: namaCoa,
            isAktif: true,
            keterangan: keterangan);
    }

    public void Update(string namaLoket, string coaId, string namaCoa, string keterangan)
    {
        NamaLoket = namaLoket?.Trim() ?? string.Empty;
        CoaId = coaId?.Trim() ?? string.Empty;
        NamaCoa = namaCoa?.Trim() ?? string.Empty;
        Keterangan = keterangan?.Trim() ?? string.Empty;

        ValidateInvariants();
    }

    public void SetAktif(bool isAktif)
    {
        IsAktif = isAktif;
    }

    public void Aktifkan() => SetAktif(true);
    public void NonAktifkan() => SetAktif(false);

    private void ValidateInvariants()
    {
        if (string.IsNullOrWhiteSpace(LoketId))
            throw new BusinessValidationException("LoketId wajib diisi");

        if (string.IsNullOrWhiteSpace(NamaLoket))
            throw new BusinessValidationException("Nama loket wajib diisi");

        if (string.IsNullOrWhiteSpace(CoaId))
            throw new BusinessValidationException("CoaId wajib diisi");
    }
}
