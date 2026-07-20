using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;

namespace Bilreg.Domain.PaymentContext.TrsBillFeature;

public interface ICreateBillDomService
{
    TrsBillType FromReg(
        TataRekeningModel tataRekening,
        RegModel reg,
        KarcisType karcis,
        JaminanType jaminan,
        PpaType dokter,
        IEnumerable<KomponenType> listReffKomp,
        DateTime createdAt = default);

    TrsBillType FromTindakan(
        TataRekeningModel tataRekening,
        TindakanModel tindakan,
        RegModel reg,
        TarifType tarif,
        JaminanType jaminan,
        IEnumerable<KomponenType> listReffKomp,
        DateTime createdAt = default);
}

public sealed class CreateBillDomService : ICreateBillDomService
{
    public TrsBillType FromReg(
        TataRekeningModel tataRekening, RegModel reg, KarcisType karcis,
        JaminanType jaminan, PpaType dokter, IEnumerable<KomponenType> listReffKomp,
        DateTime createdAt = default)
    {
        ValidateReg(tataRekening, reg.RegId);
        return TrsBillFactory.CreateFromReg(reg, karcis, jaminan, dokter, listReffKomp, createdAt);
    }

    public TrsBillType FromTindakan(
        TataRekeningModel tataRekening, TindakanModel tindakan, RegModel reg,
        TarifType tarif, JaminanType jaminan, IEnumerable<KomponenType> listReffKomp,
        DateTime createdAt = default)
    {
        ValidateReg(tataRekening, reg.RegId);
        return TrsBillFactory.CreateFromTindakan(tindakan, reg, tarif, jaminan, listReffKomp, createdAt);
    }

    private static void ValidateReg(TataRekeningModel tataRekening, string regId)
    {
        ArgumentNullException.ThrowIfNull(tataRekening);

        if (!string.Equals(tataRekening.RegId, regId, StringComparison.Ordinal))
            throw new ArgumentException(
                $"TataRekening registrasi '{tataRekening.RegId}' tidak sesuai dengan registrasi '{regId}'.",
                nameof(tataRekening));

        tataRekening.EnsureCanCreateTrsBill();
    }
}
