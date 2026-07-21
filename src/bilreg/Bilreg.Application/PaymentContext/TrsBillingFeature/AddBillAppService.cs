using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;

namespace Bilreg.Application.PaymentContext.TrsBillingFeature;

public interface IAddBillAppService
{
    TrsBillType FromReg(
        RegModel reg,
        KarcisType karcis,
        JaminanType jaminan,
        PpaType dokter,
        IEnumerable<KomponenType> listReffKomp,
        DateTime createdAt = default);

    TrsBillType FromTindakan(
        TindakanModel tindakan,
        RegModel reg,
        TarifType tarif,
        JaminanType jaminan,
        IEnumerable<KomponenType> listReffKomp,
        DateTime createdAt = default);
}


public sealed class AddBillAppService : IAddBillAppService
{
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly ICreateBillDomService _createBillDomService;

    public AddBillAppService(
        ITataRekeningRepo tataRekeningRepo,
        ITrsBillingRepo trsBillingRepo,
        ICreateBillDomService createBillDomService)
    {
        _tataRekeningRepo = tataRekeningRepo;
        _trsBillingRepo = trsBillingRepo;
        _createBillDomService = createBillDomService;
    }

    public TrsBillType FromReg(
        RegModel reg,
        KarcisType karcis,
        JaminanType jaminan,
        PpaType dokter,
        IEnumerable<KomponenType> listReffKomp,
        DateTime createdAt = default)
    {
        var (tataRekening, isNew) = ResolveTataRekening(reg);
        var bill = _createBillDomService.FromReg(tataRekening, reg, karcis, jaminan, dokter, listReffKomp, createdAt);

        if (isNew)
            _tataRekeningRepo.SaveChanges(tataRekening);

        _trsBillingRepo.SaveChanges(bill);

        return bill;
    }

    public TrsBillType FromTindakan(
        TindakanModel tindakan,
        RegModel reg,
        TarifType tarif,
        JaminanType jaminan,
        IEnumerable<KomponenType> listReffKomp,
        DateTime createdAt = default)
    {
        var (tataRekening, isNew) = ResolveTataRekening(reg);
        var bill = _createBillDomService.FromTindakan(
            tataRekening, tindakan, reg, tarif, jaminan, listReffKomp, createdAt);

        if (isNew)
            _tataRekeningRepo.SaveChanges(tataRekening);

        _trsBillingRepo.SaveChanges(bill);

        return bill;
    }

    private (TataRekeningModel Model, bool IsNew) ResolveTataRekening(RegModel reg)
    {
        var isNew = false;
        var model = _tataRekeningRepo.LoadEntity(reg).Match(
            onSome: m => m,
            onNone: () =>
            {
                isNew = true;
                return TataRekeningModel.Create(reg.RegId);
            });

        return (model, isNew);
    }
}
