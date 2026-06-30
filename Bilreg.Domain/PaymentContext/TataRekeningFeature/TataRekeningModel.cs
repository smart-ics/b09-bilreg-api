using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;

namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

/// <summary>
/// Tata Rekening aggregate — patient financial authority for one registration.
/// </summary>
public record TataRekeningModel : IRegKey
{
    private readonly List<TataRekeningPaymentType> _listTataRekeningPayment = [];
    private readonly List<TrsBillType> _listTrsBill = [];

    public TataRekeningModel(
        string regId,
        TataRekeningStatusEnum status,
        TataRekeningFinalizationType finalizationInfo,
        IEnumerable<TataRekeningPaymentType> listTataRekeningPayment,
        IEnumerable<TrsBillType> listTrsBill,
        FinancialVerificationStatusEnum financialVerificationStatus = FinancialVerificationStatusEnum.NotVerified,
        FinancialVerificationInfo? financialVerificationInfo = null,
        bool isFinancialResponsibilityAllocated = false,
        bool settlementInitiated = false)
    {
        RegId = regId;
        Status = status;
        FinalizationInfo = finalizationInfo;

        _listTataRekeningPayment = listTataRekeningPayment.ToList();
        _listTrsBill = listTrsBill.ToList();

        FinancialVerificationStatus = financialVerificationStatus;
        FinancialVerificationInfo = financialVerificationInfo;
        IsFinancialResponsibilityAllocated = isFinancialResponsibilityAllocated;
        SettlementInitiated = settlementInitiated;

        ApplyRehydrationInference();
    }

    public static TataRekeningModel Create(string regId) =>
        new(regId, TataRekeningStatusEnum.Opened, TataRekeningFinalizationType.Default, [], []);

    public string RegId { get; init; }
    public TataRekeningFinalizationType FinalizationInfo { get; private set; }
    public TataRekeningStatusEnum Status { get; private set; }
    public FinancialVerificationStatusEnum FinancialVerificationStatus { get; private set; }
    public FinancialVerificationInfo? FinancialVerificationInfo { get; private set; }
    public bool IsFinancialResponsibilityAllocated { get; private set; }
    public bool SettlementInitiated { get; private set; }
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

        if (Status != TataRekeningStatusEnum.Opened)
            throw new InvalidOperationException(
                "TataRekening can not be closed since current status is not OPENED.");

        Status = TataRekeningStatusEnum.Closed;
        ResetFinancialVerification();
    }

    public void ReOpen()
    {
        EnsureNotLunas();

        if (Status != TataRekeningStatusEnum.Closed)
            throw new InvalidOperationException(
                "TataRekening can not be re-opened since current status is not CLOSED.");

        ClearFinancialResponsibilityAllocation();
        ResetFinancialVerification();
        SettlementInitiated = false;
        Status = TataRekeningStatusEnum.Opened;
    }

    public void CompleteFinancialVerification(string petugasVerif, DateTime verifiedAt)
    {
        EnsureNotLunas();
        EnsureStatusClosed();

        if (FinancialVerificationStatus != FinancialVerificationStatusEnum.NotVerified)
            throw new InvalidOperationException(
                "Financial Verification hanya dapat diselesaikan saat belum diverifikasi.");

        if (string.IsNullOrWhiteSpace(petugasVerif))
            throw new ArgumentException("Petugas verifikator tidak boleh kosong.", nameof(petugasVerif));

        FinancialVerificationStatus = FinancialVerificationStatusEnum.Valid;
        FinancialVerificationInfo = new FinancialVerificationInfo(petugasVerif, verifiedAt);
    }

    public void RequireFinancialAdjustment()
    {
        EnsureNotLunas();
        EnsureStatusClosed();

        FinancialVerificationStatus = FinancialVerificationStatusEnum.RequiresAdjustment;
        FinancialVerificationInfo = null;
    }

    public void AllocateFinancialResponsibility(IEnumerable<TataRekeningPaymentType> listPayment)
    {
        EnsureNotLunas();
        EnsureCanAllocate();
        EnsureListTrsBillNotEmpty();

        var payments = listPayment.ToList();
        ValidateFinalizationTotals(payments);

        ClearFinancialResponsibilityAllocation();
        _listTataRekeningPayment.AddRange(payments);
        FinalizationAllocation();
        AssertFinalizationComplete();
        IsFinancialResponsibilityAllocated = true;
    }

    /// <summary>
    /// Locks pre-validated Financial Responsibility Allocation and transitions to FINALIZED.
    /// </summary>
    public void FinalizeFinancialResponsibility(string petugasVerif, DateTime finalizationDate)
    {
        EnsureNotLunas();
        EnsureCanFinalize();
        EnsureListTrsBillNotEmpty();

        if (string.IsNullOrWhiteSpace(petugasVerif))
            throw new ArgumentException("Petugas verifikator tidak boleh kosong.", nameof(petugasVerif));

        FinalizationInfo = new TataRekeningFinalizationType(petugasVerif, finalizationDate);
        AssertFinalizationComplete();
        Status = TataRekeningStatusEnum.Finalized;
    }

    public void InitiateSettlement(string petugasVerif, DateTime initiatedAt)
    {
        EnsureNotLunas();

        if (Status != TataRekeningStatusEnum.Finalized)
            throw new InvalidOperationException(
                "Settlement Initiation hanya dapat dilakukan saat TataRekening berstatus FINALIZED.");

        if (!IsFinancialResponsibilityAllocated)
            throw new InvalidOperationException(
                "Settlement Initiation memerlukan Financial Responsibility Allocation yang lengkap.");

        if (SettlementInitiated)
            throw new InvalidOperationException(
                "Settlement Initiation sudah dilakukan sebelumnya.");

        EnsureListTrsBillNotEmpty();

        if (_listTrsBill.Any(bill => bill.ListPayment.Any()))
            throw new InvalidOperationException(
                "Settlement Initiation tidak dapat dilakukan karena Payment Settlement sudah dimulai.");

        if (string.IsNullOrWhiteSpace(petugasVerif))
            throw new ArgumentException("Petugas verifikator tidak boleh kosong.", nameof(petugasVerif));

        SettlementInitiated = true;
    }

    public void CancelFinalization()
    {
        EnsureNotLunas();

        if (Status != TataRekeningStatusEnum.Finalized)
            throw new InvalidOperationException(
                "TataRekening tidak dapat cancel finalization kecuali berstatus FINALIZED.");

        EnsureListTrsBillNotEmpty();

        if (_listTrsBill.Any(bill => bill.ListPayment.Any()))
            throw new InvalidOperationException(
                "Tidak dapat cancel finalization karena sudah ada pembayaran pada list TrsBill.");

        ClearFinancialResponsibilityAllocation();
        FinalizationInfo = TataRekeningFinalizationType.Default;
        SettlementInitiated = false;
        Status = TataRekeningStatusEnum.Closed;
    }

    public void EnsureCanCreateTrsBill() => EnsureOpenForBillMutation();

    public void EnsureCanDeleteBill() => EnsureOpenForBillMutation();

    #region LegacyCashierBridge

    /// <summary>
    /// LEGACY: Payment Settlement belongs to Cashier (SOP Kasir).
    /// Retained for backward compatibility only — new workflow uses InitiateSettlement + Cashier.
    /// </summary>
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

    #endregion

    private void ApplyRehydrationInference()
    {
        if (_listTrsBill.Any(bill => bill.ListFinalization.Any()) ||
            Status is TataRekeningStatusEnum.Finalized or TataRekeningStatusEnum.Lunas)
        {
            IsFinancialResponsibilityAllocated = true;
        }

        if (FinancialVerificationStatus == FinancialVerificationStatusEnum.NotVerified &&
            Status is TataRekeningStatusEnum.Closed or TataRekeningStatusEnum.Finalized or TataRekeningStatusEnum.Lunas &&
            (IsFinancialResponsibilityAllocated || Status >= TataRekeningStatusEnum.Finalized))
        {
            FinancialVerificationStatus = FinancialVerificationStatusEnum.Valid;
        }
    }

    private void ResetFinancialVerification()
    {
        FinancialVerificationStatus = FinancialVerificationStatusEnum.NotVerified;
        FinancialVerificationInfo = null;
    }

    private void ClearFinancialResponsibilityAllocation()
    {
        foreach (var bill in _listTrsBill)
            bill.CancelFinalization();

        _listTataRekeningPayment.Clear();
        IsFinancialResponsibilityAllocated = false;
    }

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

    private void EnsureStatusClosed()
    {
        if (Status != TataRekeningStatusEnum.Closed)
            throw new InvalidOperationException(
                "Operasi ini hanya diizinkan saat TataRekening berstatus CLOSED.");
    }

    private void EnsureListTrsBillNotEmpty()
    {
        if (_listTrsBill.Count == 0)
            throw new InvalidOperationException(
                "Operasi lifecycle memerlukan list TrsBill yang tidak kosong.");
    }

    private void EnsureCanAllocate()
    {
        EnsureStatusClosed();

        if (FinancialVerificationStatus == FinancialVerificationStatusEnum.RequiresAdjustment)
            throw new InvalidOperationException(
                "Financial Responsibility Allocation tidak dapat dilakukan karena memerlukan Financial Adjustment.");

        if (FinancialVerificationStatus != FinancialVerificationStatusEnum.Valid)
            throw new InvalidOperationException(
                "Financial Responsibility Allocation memerlukan Financial Verification yang valid.");
    }

    private void EnsureCanFinalize()
    {
        if (Status == TataRekeningStatusEnum.Opened)
            throw new InvalidOperationException(
                "Tanggungan keuangan tidak dapat difinalisasi karena TataRekening masih OPEN.");

        if (Status == TataRekeningStatusEnum.Finalized)
            throw new InvalidOperationException(
                "Tanggungan keuangan sudah difinalisasi.");

        if (Status == TataRekeningStatusEnum.Lunas)
            throw new InvalidOperationException(
                "Tanggungan keuangan tidak dapat difinalisasi karena TataRekening sudah LUNAS.");

        if (Status != TataRekeningStatusEnum.Closed)
            throw new InvalidOperationException(
                "Tanggungan keuangan hanya dapat difinalisasi saat TataRekening berstatus CLOSED.");

        if (FinancialVerificationStatus != FinancialVerificationStatusEnum.Valid)
            throw new InvalidOperationException(
                "Finalization memerlukan Financial Verification yang valid.");

        if (!IsFinancialResponsibilityAllocated)
            throw new InvalidOperationException(
                "Finalization memerlukan Financial Responsibility Allocation yang lengkap.");
    }

    private decimal TotalBillByModul(BillModulGroup modulGroup) =>
        _listTrsBill.Where(x => x.ModulGroup == modulGroup).Sum(x => x.Nilai.Total);

    private void ValidateFinalizationTotals(IReadOnlyList<TataRekeningPaymentType> listPayment)
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
                    var finalized = bill.ListFinalization.Sum(d => d.Nilai);
                    var paid = bill.ListPayment.Sum(p => p.Nilai);
                    return finalized - paid;
                });

            if (paymentTotal > outstanding)
                throw new InvalidOperationException(
                    $"Total pembayaran {modulGroup} ({paymentTotal}) melebihi sisa tanggungan ({outstanding}).");
        }
    }

    private void FinalizationAllocation()
    {
        var trsBayarId = $"RO{(RegId.Length >= 8 ? RegId[^8..] : RegId)}";
        var totalJasaTrans = TotalBillByModul(BillModulGroup.Jasa);
        var totalObatTrans = TotalBillByModul(BillModulGroup.Obat);
        var petugasVerif = FinalizationInfo.PetugasVerif;
        var allocationDate = FinalizationInfo.FinalizationDate;

        foreach (var item in _listTataRekeningPayment)
        {
            AllocateFinalizationToModuleGroup(
                item, item.NilaiJasa, BillModulGroup.Jasa, totalJasaTrans, trsBayarId, petugasVerif, allocationDate);

            AllocateFinalizationToModuleGroup(
                item, item.NilaiObat, BillModulGroup.Obat, totalObatTrans, trsBayarId, petugasVerif, allocationDate);
        }
    }

    private void AllocateFinalizationToModuleGroup(
        TataRekeningPaymentType item,
        decimal modulAllocation,
        BillModulGroup modulGroup,
        decimal modulTotal,
        string trsBayarId,
        string petugasVerif,
        DateTime allocationDate)
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

            bill.FinalizeAllocation(
                item.Payment, share, petugasVerif, trsBayarId, allocationDate);
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
            .Sum(bill => bill.ListFinalization.Sum(d => d.Nilai) - bill.ListPayment.Sum(p => p.Nilai));

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
                Outstanding = bill.ListFinalization.Sum(d => d.Nilai) - bill.ListPayment.Sum(p => p.Nilai)
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

    private void AssertFinalizationComplete()
    {
        foreach (var bill in _listTrsBill)
        {
            var finalized = bill.ListFinalization.Sum(x => x.Nilai);
            if (finalized != bill.Nilai.Total)
                throw new InvalidOperationException(
                    $"Finalisasi pada bill '{bill.TrsBillingId}' tidak lengkap: {finalized} dari {bill.Nilai.Total}.");
        }
    }

    private void TryTransitionToLunas()
    {
        if (Status != TataRekeningStatusEnum.Finalized)
            return;

        foreach (var bill in _listTrsBill)
        {
            var totalFinalized = bill.ListFinalization.Sum(x => x.Nilai);
            var totalPaid = bill.ListPayment.Sum(x => x.Nilai);
            if (totalPaid < totalFinalized)
                return;
        }

        Status = TataRekeningStatusEnum.Lunas;
    }
}

/// <summary>
/// Finalization metadata recorded when financial responsibility is finalized.
/// </summary>
public record TataRekeningFinalizationType(string PetugasVerif, DateTime FinalizationDate)
{
    public static TataRekeningFinalizationType Default => new("-", new DateTime(3000, 1, 1));
}
