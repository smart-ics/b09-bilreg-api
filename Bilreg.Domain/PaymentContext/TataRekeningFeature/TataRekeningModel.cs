using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;

namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

public record TataRekeningModel : IRegKey
{
    private readonly List<TataRekeningPaymentType> _listTataRekeningPayment = [];
    private readonly List<TrsBillType> _listTrsBill = [];
    
    public TataRekeningModel(string regId, TataRekeningStatusEnum status,
        TataRekeningDischargeType dischargeInfo,
        IEnumerable<TataRekeningPaymentType> listTataRekeningPayment,
        IEnumerable<TrsBillType> listTrsBill)
    {
        RegId = regId;
        Status = status;
        DischargeInfo = dischargeInfo;

        _listTataRekeningPayment = listTataRekeningPayment.ToList();
        _listTrsBill = listTrsBill.ToList();
    }

    public static TataRekeningModel Create(string regId)
    {
        return new TataRekeningModel(regId, TataRekeningStatusEnum.Opened,
            TataRekeningDischargeType.Default, [], []);
    }

    public string RegId { get; init; }
    public TataRekeningDischargeType DischargeInfo { get; private set; }
    public TataRekeningStatusEnum Status { get; private set; }
    public IEnumerable<TataRekeningPaymentType> ListPayment => _listTataRekeningPayment;
    public IEnumerable<TrsBillType> ListTrsBill => _listTrsBill;

    public void Close()
    {
        if (Status == TataRekeningStatusEnum.Opened)
            Status = TataRekeningStatusEnum.Closed;
        else
            throw new InvalidOperationException(
                $"TataRekening can not be closed since current status is not OPENED");
    }

    public void ReOpen()
    {
        if (Status == TataRekeningStatusEnum.Closed)
            Status = TataRekeningStatusEnum.Opened;
        else
            throw new InvalidOperationException(
                $"TataRekening can not be re-opened since current status is not CLOSED");
    }

    public void Discharge(IEnumerable<TataRekeningPaymentType> listPayment, string petugasKair, DateTime dischargeDate)
    {
        //  GUARD
        EnsureCanDischarge();
        var totalJasaPayment = listPayment.Sum(x => x.NilaiJasa);
        var totalObatPayment = listPayment.Sum(x => x.NilaiObat);
        var totalJasaTrans = _listTrsBill.Where(x => x.Modul == 0).Sum(x => x.Nilai.Total);
        var totalObatTrans = _listTrsBill.Where(x => x.Modul == 1).Sum(x => x.Nilai.Total);
        if (totalJasaPayment != totalJasaTrans)
            throw new InvalidOperationException(
                $"Total Jasa pada payment ({totalJasaPayment}) tidak sama dengan total Jasa pada transaksi ({totalJasaTrans}).");
        if (totalObatPayment != totalObatTrans)
            throw new InvalidOperationException(
                $"Total Obat pada payment ({totalObatPayment}) tidak sama dengan total Obat pada transaksi ({totalObatTrans}).");
        
        //  BUILD
        _listTataRekeningPayment.Clear();
        _listTataRekeningPayment.AddRange(listPayment);
        DischargeInfo = new TataRekeningDischargeType(petugasKair, dischargeDate);
        PaymentAllocation();
        Status = TataRekeningStatusEnum.Finalized;
    }

    private void EnsureCanDischarge()
    {
        if (Status == TataRekeningStatusEnum.Opened)
            throw new InvalidOperationException(
                $"TataRekening tidak dapat discharge karena masih open.");
        if (Status == TataRekeningStatusEnum.Finalized)
            throw new InvalidOperationException(
                $"TataRekening tidak dapat discharge karena sudah finalized.");
        if (Status == TataRekeningStatusEnum.Paid)
            throw new InvalidOperationException(
                $"TataRekening tidak dapat discharge karena sudah paid.");
    }


    private void PaymentAllocation()
    {
        var trsBayarId = $"RO{RegId[^8..]}";
        var totalJasaTrans = _listTrsBill.Where(x => x.Modul == 0).Sum(x => x.Nilai.Total);
        var totalObatTrans = _listTrsBill.Where(x => x.Modul == 1).Sum(x => x.Nilai.Total);
        foreach (var item in _listTataRekeningPayment)
        {
            var jasaAlokasi = item.NilaiJasa;
            foreach (var item2 in _listTrsBill.Where(x => x.Modul == 0))
            {
                var jasaDischarge = jasaAlokasi * item2.Nilai.Total / totalJasaTrans;
                item2.Discharge(item.Payment, jasaDischarge, DischargeInfo.PetugasKair, trsBayarId, DischargeInfo.DischargeDate);
            }

            var obatAlokasi = item.NilaiObat;
            foreach (var item2 in _listTrsBill.Where(x => x.Modul == 1))
            {
                var obatDischarge = obatAlokasi * item2.Nilai.Total / totalObatTrans;
                item2.Discharge(item.Payment, obatDischarge, DischargeInfo.PetugasKair, trsBayarId, DischargeInfo.DischargeDate);
            }
        }
    }

    public void EnsureCanCreateTrsBill()
    {
        if (Status == TataRekeningStatusEnum.Closed)
            throw new InvalidOperationException(
                $"TrsBillType tidak dapat dibuat karena TataRekening untuk registrasi '{RegId}' berstatus Closed.");
    }
}

public record TataRekeningDischargeType(string PetugasKair, DateTime DischargeDate)
{
    public static TataRekeningDischargeType Default => new("-", new DateTime(3000,1,1));
};
