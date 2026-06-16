using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;

namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

/// <summary>
/// Patient financial authority — owns billing lifecycle and list TrsBill.
/// </summary>
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

    public void DeleteBill(string trsBillingId)
    {
        EnsureNotLunas();
        EnsureOpenForBillMutation();

        var index = _listTrsBill.FindIndex(x => x.TrsBillingId == trsBillingId);
        if (index < 0)
            throw new InvalidOperationException(
                $"TrsBill '{trsBillingId}' tidak ditemukan pada list TrsBill.");

        _listTrsBill.RemoveAt(index);
    }

    public void Close()
    {
        EnsureNotLunas();

        if (Status == TataRekeningStatusEnum.Opened)
            Status = TataRekeningStatusEnum.Closed;
        else
            throw new InvalidOperationException(
                "TataRekening can not be closed since current status is not OPENED.");
    }

    public void ReOpen()
    {
        EnsureNotLunas();

        if (Status == TataRekeningStatusEnum.Closed)
            Status = TataRekeningStatusEnum.Opened;
        else
            throw new InvalidOperationException(
                "TataRekening can not be re-opened since current status is not CLOSED.");
    }

    public void Discharge(IEnumerable<TataRekeningPaymentType> listPayment, string petugasKair, DateTime dischargeDate)
    {
        EnsureNotLunas();
        EnsureCanDischarge();
        EnsureListTrsBillNotEmpty();

        var payments = listPayment.ToList();
        ValidateDischargeTotals(payments);

        _listTataRekeningPayment.Clear();
        _listTataRekeningPayment.AddRange(payments);
        DischargeInfo = new TataRekeningDischargeType(petugasKair, dischargeDate);
        DischargeAllocation();
        AssertDischargeComplete();
        Status = TataRekeningStatusEnum.Finalized;
    }

    public void Pay(IEnumerable<TataRekeningPaymentType> listPayment, string trsBayarId, DateTime tglBayar)
    {
        EnsureNotLunas();

        if (Status != TataRekeningStatusEnum.Finalized)
            throw new InvalidOperationException(
                "TataRekening tidak dapat menerima pembayaran kecuali berstatus FINALIZED.");

        EnsureListTrsBillNotEmpty();

        if (string.IsNullOrWhiteSpace(trsBayarId))
            throw new ArgumentException("Trs bayar id should not be empty", nameof(trsBayarId));

        var payments = listPayment.ToList();
        ValidatePaymentTotals(payments);
        PaymentAllocation(payments, trsBayarId, tglBayar);
        TryTransitionToLunas();
    }

    public void CancelDischarge()
    {
        EnsureNotLunas();

        if (Status != TataRekeningStatusEnum.Finalized)
            throw new InvalidOperationException(
                "TataRekening tidak dapat cancel discharge kecuali berstatus FINALIZED.");

        EnsureListTrsBillNotEmpty();

        if (_listTrsBill.Any(bill => bill.ListPayment.Any()))
            throw new InvalidOperationException(
                "Tidak dapat cancel discharge karena sudah ada pembayaran pada list TrsBill.");

        foreach (var bill in _listTrsBill)
            bill.CancelDischarge();

        _listTataRekeningPayment.Clear();
        DischargeInfo = TataRekeningDischargeType.Default;
        Status = TataRekeningStatusEnum.Closed;
    }

    public void EnsureCanCreateTrsBill() => EnsureOpenForBillMutation();

    public void EnsureCanDeleteBill() => EnsureOpenForBillMutation();

    private void EnsureOpenForBillMutation()
    {
        if (Status != TataRekeningStatusEnum.Opened)
            throw new InvalidOperationException(
                $"Operasi bill hanya diizinkan saat TataRekening untuk registrasi '{RegId}' berstatus OPEN.");
    }

    private void EnsureNotLunas()
    {
        if (Status == TataRekeningStatusEnum.Lunas)
            throw new InvalidOperationException(
                "TataRekening sudah LUNAS dan tidak dapat dimodifikasi.");
    }

    private void EnsureListTrsBillNotEmpty()
    {
        if (_listTrsBill.Count == 0)
            throw new InvalidOperationException(
                "Operasi lifecycle memerlukan list TrsBill yang tidak kosong.");
    }

    private void EnsureCanDischarge()
    {
        if (Status == TataRekeningStatusEnum.Opened)
            throw new InvalidOperationException(
                "TataRekening tidak dapat discharge karena masih open.");

        if (Status == TataRekeningStatusEnum.Finalized)
            throw new InvalidOperationException(
                "TataRekening tidak dapat discharge karena sudah finalized.");

        if (Status == TataRekeningStatusEnum.Lunas)
            throw new InvalidOperationException(
                "TataRekening tidak dapat discharge karena sudah lunas.");
    }

    private decimal TotalBillByModul(BillModulGroup modulGroup) =>
        _listTrsBill.Where(x => x.ModulGroup == modulGroup).Sum(x => x.Nilai.Total);

    private void ValidateDischargeTotals(IReadOnlyList<TataRekeningPaymentType> listPayment)
    {
        var totalJasaPayment = listPayment.Sum(x => x.NilaiJasa);
        var totalObatPayment = listPayment.Sum(x => x.NilaiObat);
        var totalJasaTrans = TotalBillByModul(BillModulGroup.Jasa);
        var totalObatTrans = TotalBillByModul(BillModulGroup.Obat);

        if (totalJasaPayment != totalJasaTrans)
            throw new InvalidOperationException(
                $"Total Jasa pada payment ({totalJasaPayment}) tidak sama dengan total Jasa pada transaksi ({totalJasaTrans}).");

        if (totalObatPayment != totalObatTrans)
            throw new InvalidOperationException(
                $"Total Obat pada payment ({totalObatPayment}) tidak sama dengan total Obat pada transaksi ({totalObatTrans}).");
    }

    private void ValidatePaymentTotals(IReadOnlyList<TataRekeningPaymentType> listPayment)
    {
        foreach (var modulGroup in new[] { BillModulGroup.Jasa, BillModulGroup.Obat })
        {
            var paymentTotal = listPayment.Sum(x =>
                modulGroup == BillModulGroup.Jasa ? x.NilaiJasa : x.NilaiObat);

            var outstanding = _listTrsBill
                .Where(x => x.ModulGroup == modulGroup)
                .Sum(bill =>
                {
                    var discharged = bill.ListDischarge.Sum(d => d.Nilai);
                    var paid = bill.ListPayment.Sum(p => p.Nilai);
                    return discharged - paid;
                });

            if (paymentTotal > outstanding)
                throw new InvalidOperationException(
                    $"Total pembayaran {modulGroup} ({paymentTotal}) melebihi sisa tanggungan ({outstanding}).");
        }
    }

    private void DischargeAllocation()
    {
        var trsBayarId = $"RO{(RegId.Length >= 8 ? RegId[^8..] : RegId)}";
        var totalJasaTrans = TotalBillByModul(BillModulGroup.Jasa);
        var totalObatTrans = TotalBillByModul(BillModulGroup.Obat);

        foreach (var item in _listTataRekeningPayment)
        {
            AllocateDischargeToModulGroup(
                item, item.NilaiJasa, BillModulGroup.Jasa, totalJasaTrans, trsBayarId);

            AllocateDischargeToModulGroup(
                item, item.NilaiObat, BillModulGroup.Obat, totalObatTrans, trsBayarId);
        }
    }

    private void AllocateDischargeToModulGroup(
        TataRekeningPaymentType item,
        decimal modulAllocation,
        BillModulGroup modulGroup,
        decimal modulTotal,
        string trsBayarId)
    {
        if (modulAllocation == 0)
            return;

        if (modulTotal == 0)
            throw new InvalidOperationException(
                $"Alokasi {modulGroup} ({modulAllocation}) tidak dapat didistribusikan karena tidak ada bill {modulGroup}.");

        var bills = _listTrsBill.Where(x => x.ModulGroup == modulGroup).ToList();
        var allocated = 0m;

        for (var i = 0; i < bills.Count; i++)
        {
            var bill = bills[i];
            var share = i == bills.Count - 1
                ? modulAllocation - allocated
                : modulAllocation * bill.Nilai.Total / modulTotal;

            bill.Discharge(
                item.Payment, share, DischargeInfo.PetugasKair, trsBayarId, DischargeInfo.DischargeDate);
            allocated += share;
        }
    }

    private void PaymentAllocation(
        IReadOnlyList<TataRekeningPaymentType> listPayment,
        string trsBayarId,
        DateTime tglBayar)
    {
        var totalJasaOutstanding = OutstandingByModul(BillModulGroup.Jasa);
        var totalObatOutstanding = OutstandingByModul(BillModulGroup.Obat);

        foreach (var item in listPayment)
        {
            AllocatePaymentToModulGroup(
                item, item.NilaiJasa, BillModulGroup.Jasa, totalJasaOutstanding, trsBayarId, tglBayar);

            AllocatePaymentToModulGroup(
                item, item.NilaiObat, BillModulGroup.Obat, totalObatOutstanding, trsBayarId, tglBayar);
        }
    }

    private decimal OutstandingByModul(BillModulGroup modulGroup) =>
        _listTrsBill
            .Where(x => x.ModulGroup == modulGroup)
            .Sum(bill => bill.ListDischarge.Sum(d => d.Nilai) - bill.ListPayment.Sum(p => p.Nilai));

    private void AllocatePaymentToModulGroup(
        TataRekeningPaymentType item,
        decimal modulAllocation,
        BillModulGroup modulGroup,
        decimal modulOutstanding,
        string trsBayarId,
        DateTime tglBayar)
    {
        if (modulAllocation == 0)
            return;

        if (modulOutstanding == 0)
            throw new InvalidOperationException(
                $"Pembayaran {modulGroup} ({modulAllocation}) tidak dapat didistribusikan karena tidak ada tanggungan {modulGroup}.");

        var bills = _listTrsBill
            .Where(x => x.ModulGroup == modulGroup)
            .Select(bill => new
            {
                Bill = bill,
                Outstanding = bill.ListDischarge.Sum(d => d.Nilai) - bill.ListPayment.Sum(p => p.Nilai)
            })
            .Where(x => x.Outstanding > 0)
            .ToList();

        var allocated = 0m;

        for (var i = 0; i < bills.Count; i++)
        {
            var bill = bills[i].Bill;
            var billOutstanding = bills[i].Outstanding;

            var share = i == bills.Count - 1
                ? modulAllocation - allocated
                : modulAllocation * billOutstanding / modulOutstanding;

            bill.Pay(item.Payment, share, trsBayarId, tglBayar);
            allocated += share;
        }
    }

    private void AssertDischargeComplete()
    {
        foreach (var bill in _listTrsBill)
        {
            var discharged = bill.ListDischarge.Sum(x => x.Nilai);
            if (discharged != bill.Nilai.Total)
                throw new InvalidOperationException(
                    $"Discharge pada bill '{bill.TrsBillingId}' tidak lengkap: {discharged} dari {bill.Nilai.Total}.");
        }
    }

    private void TryTransitionToLunas()
    {
        if (Status != TataRekeningStatusEnum.Finalized)
            return;

        foreach (var bill in _listTrsBill)
        {
            var totalDischarged = bill.ListDischarge.Sum(x => x.Nilai);
            var totalPaid = bill.ListPayment.Sum(x => x.Nilai);
            if (totalPaid < totalDischarged)
                return;
        }

        Status = TataRekeningStatusEnum.Lunas;
    }
}

public record TataRekeningDischargeType(string PetugasKair, DateTime DischargeDate)
{
    public static TataRekeningDischargeType Default => new("-", new DateTime(3000, 1, 1));
}
