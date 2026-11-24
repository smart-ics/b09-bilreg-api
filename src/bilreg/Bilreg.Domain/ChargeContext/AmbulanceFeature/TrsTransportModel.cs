using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.ChargeContext.AmbulanceFeature;

public class TrsTransportModel : ITrsTransportKey
{
    public const string ID_PREFIX = "TP";

    private readonly List<TrsTransportItemModel> _listItems = [];

    // Header & Keys
    public string TrsTransportId { get; protected set; }
    public string RegId { get; protected set; } = string.Empty;
    public string LayananId { get; protected set; } = string.Empty;
    public DateTime TglTrs { get; protected set; }
    public string PetugasId { get; protected set; } = string.Empty;

    // Void info
    public bool IsVoid { get; protected set; }
    public DateTime TglVoid { get; protected set; }
    public string PetugasVoidId { get; protected set; } = string.Empty;

    // Vehicle & Driver Assignment
    public string AmbulanceId { get; protected set; } = string.Empty;
    public string DriverId { get; protected set; } = string.Empty;
    public string NoPolisi { get; protected set; } = string.Empty;

    // Route & Tariff Method (Core Invariant 6)
    public string TujuanTransportId { get; protected set; } = string.Empty;
    public decimal Konstanta { get; protected set; } = 0m;
    public ChargingMethodEnum MetodeTarif { get; protected set; } = ChargingMethodEnum.Wilayah;

    // Totals
    public decimal SubTotal { get; protected set; } = 0m;
    public decimal Diskon { get; protected set; } = 0m;
    public decimal Total { get; protected set; } = 0m;

    // Legacy Cashier Fields (Compatibility with clsTrsKasir & ta_trs_transport)
    public decimal Kas { get; protected set; } = 0m;
    public string RekKasId { get; protected set; } = string.Empty;
    public decimal Bank { get; protected set; } = 0m;
    public string RekBankId { get; protected set; } = string.Empty;
    public decimal KartuKredit { get; protected set; } = 0m;
    public string RekKartuKreditId { get; protected set; } = string.Empty;
    public string NoKartuKredit { get; protected set; } = string.Empty;
    public string NoBank { get; protected set; } = string.Empty;
    public string JenisKartuKreditId { get; protected set; } = string.Empty;
    public string JenisBankId { get; protected set; } = string.Empty;
    public decimal Sisa { get; protected set; } = 0m;
    public string RekSisaId { get; protected set; } = string.Empty;
    public string PetugasKasirId { get; protected set; } = string.Empty;
    public string KelasId { get; protected set; } = string.Empty;
    public string TrsChargeId { get; protected set; } = string.Empty;
    public string TrsDepositId { get; protected set; } = string.Empty;
    public string TrsKwitansiId { get; protected set; } = string.Empty;
    public decimal GenKeu { get; protected set; } = 0m;
    public string TipeJaminanId { get; protected set; } = string.Empty;

    // Patient Demographics
    public string PasienName { get; protected set; } = string.Empty;
    public string Alm1Pasien { get; protected set; } = string.Empty;
    public string Alm2Pasien { get; protected set; } = string.Empty;
    public string KotaPasien { get; protected set; } = string.Empty;
    public string Sex { get; protected set; } = string.Empty;
    public string Keterangan { get; protected set; } = string.Empty;

    // Extended Ambulance Feature Fields
    public TransportStatusEnum Status { get; protected set; } = TransportStatusEnum.Requested;
    public TransportTypeEnum TipePerjalanan { get; protected set; } = TransportTypeEnum.OutboundReferral;
    public string PerawatId { get; protected set; } = string.Empty;
    public string DokterId { get; protected set; } = string.Empty;
    public string LokasiAsal { get; protected set; } = string.Empty;
    public string LokasiTujuan { get; protected set; } = string.Empty;

    // Logistics Timestamps
    public DateTime TglRequest { get; protected set; }
    public DateTime? TglDispatch { get; protected set; }
    public DateTime? TglArrival { get; protected set; }
    public DateTime? TglReturn { get; protected set; }

    // Odometers
    public decimal StartOdometer { get; protected set; } = 0m;
    public decimal EndOdometer { get; protected set; } = 0m;
    public decimal TotalJarakKm => Math.Max(0m, EndOdometer - StartOdometer);

    // Billing & Settlement Flags
    public string JaminanId { get; protected set; } = string.Empty;
    public bool IsNonReg { get; protected set; } = false;
    public bool IsPostedBilling { get; protected set; } = false;

    // Cancellation
    public string AlasanBatal { get; protected set; } = string.Empty;
    public decimal BiayaBatal { get; protected set; } = 0m;

    // Items
    public IReadOnlyCollection<TrsTransportItemModel> ListItems => _listItems.AsReadOnly();

    protected TrsTransportModel(string trsTransportId)
    {
        TrsTransportId = trsTransportId;
    }

    #region FACTORY / CREATION
    public static TrsTransportModel Create(
        TransportTypeEnum tipePerjalanan,
        ChargingMethodEnum metodeTarif,
        string layananId,
        string petugasId,
        string? trsTransportId = null,
        DateTime? tglTrs = null,
        string regId = "",
        string tujuanTransportId = "",
        decimal konstanta = 0m,
        string lokasiAsal = "",
        string lokasiTujuan = "",
        string jaminanId = "",
        string keterangan = "",
        string ambulanceId = "",
        string driverId = "",
        string noPolisi = "",
        string perawatId = "",
        string dokterId = "",
        decimal totalJarakKm = 0m)
    {
        var id = string.IsNullOrWhiteSpace(trsTransportId)
            ? NunaId.NewLegacyCompact(ID_PREFIX)
            : trsTransportId;

        var model = new TrsTransportModel(id)
        {
            TipePerjalanan = tipePerjalanan,
            LayananId = layananId ?? string.Empty,
            PetugasId = petugasId ?? string.Empty,
            TglTrs = tglTrs ?? DateTime.Now,
            TglRequest = tglTrs ?? DateTime.Now,
            RegId = regId ?? string.Empty,
            LokasiAsal = lokasiAsal ?? string.Empty,
            LokasiTujuan = lokasiTujuan ?? string.Empty,
            JaminanId = jaminanId ?? string.Empty,
            Keterangan = keterangan ?? string.Empty,
            AmbulanceId = ambulanceId ?? string.Empty,
            DriverId = driverId ?? string.Empty,
            NoPolisi = noPolisi ?? string.Empty,
            PerawatId = perawatId ?? string.Empty,
            DokterId = dokterId ?? string.Empty,
            Status = TransportStatusEnum.Requested,
            IsNonReg = string.IsNullOrWhiteSpace(regId)
        };

        if (metodeTarif == ChargingMethodEnum.Wilayah)
        {
            model.SetMetodeWilayah(tujuanTransportId, konstanta);
        }
        else
        {
            model.SetMetodeJarak(totalJarakKm);
        }

        return model;
    }

    public static TrsTransportModel Rehydrate(
        string trsTransportId,
        string regId,
        string layananId,
        DateTime tglTrs,
        string petugasId,
        bool isVoid,
        DateTime tglVoid,
        string petugasVoidId,
        string ambulanceId,
        string driverId,
        string noPolisi,
        string tujuanTransportId,
        decimal konstanta,
        decimal subTotal,
        decimal diskon,
        decimal total,
        decimal kas,
        string rekKasId,
        decimal bank,
        string rekBankId,
        decimal kartuKredit,
        string rekKartuKreditId,
        string noKartuKredit,
        string noBank,
        string jenisKartuKreditId,
        string jenisBankId,
        decimal sisa,
        string rekSisaId,
        string petugasKasirId,
        string kelasId,
        string trsChargeId,
        string trsDepositId,
        string trsKwitansiId,
        decimal genKeu,
        string tipeJaminanId,
        string pasienName,
        string alm1Pasien,
        string alm2Pasien,
        string kotaPasien,
        string sex,
        string keterangan,
        TransportStatusEnum status,
        TransportTypeEnum tipePerjalanan,
        ChargingMethodEnum metodeTarif,
        string perawatId,
        string dokterId,
        string lokasiAsal,
        string lokasiTujuan,
        DateTime tglRequest,
        DateTime? tglDispatch,
        DateTime? tglArrival,
        DateTime? tglReturn,
        decimal startOdometer,
        decimal endOdometer,
        string jaminanId,
        bool isNonReg,
        bool isPostedBilling,
        string alasanBatal,
        decimal biayaBatal,
        IEnumerable<TrsTransportItemModel>? listItems = null)
    {
        var model = new TrsTransportModel(trsTransportId)
        {
            RegId = regId ?? string.Empty,
            LayananId = layananId ?? string.Empty,
            TglTrs = tglTrs,
            PetugasId = petugasId ?? string.Empty,
            IsVoid = isVoid,
            TglVoid = tglVoid,
            PetugasVoidId = petugasVoidId ?? string.Empty,
            AmbulanceId = ambulanceId ?? string.Empty,
            DriverId = driverId ?? string.Empty,
            NoPolisi = noPolisi ?? string.Empty,
            TujuanTransportId = tujuanTransportId ?? string.Empty,
            Konstanta = konstanta,
            SubTotal = subTotal,
            Diskon = diskon,
            Total = total,
            Kas = kas,
            RekKasId = rekKasId ?? string.Empty,
            Bank = bank,
            RekBankId = rekBankId ?? string.Empty,
            KartuKredit = kartuKredit,
            RekKartuKreditId = rekKartuKreditId ?? string.Empty,
            NoKartuKredit = noKartuKredit ?? string.Empty,
            NoBank = noBank ?? string.Empty,
            JenisKartuKreditId = jenisKartuKreditId ?? string.Empty,
            JenisBankId = jenisBankId ?? string.Empty,
            Sisa = sisa,
            RekSisaId = rekSisaId ?? string.Empty,
            PetugasKasirId = petugasKasirId ?? string.Empty,
            KelasId = kelasId ?? string.Empty,
            TrsChargeId = trsChargeId ?? string.Empty,
            TrsDepositId = trsDepositId ?? string.Empty,
            TrsKwitansiId = trsKwitansiId ?? string.Empty,
            GenKeu = genKeu,
            TipeJaminanId = tipeJaminanId ?? string.Empty,
            PasienName = pasienName ?? string.Empty,
            Alm1Pasien = alm1Pasien ?? string.Empty,
            Alm2Pasien = alm2Pasien ?? string.Empty,
            KotaPasien = kotaPasien ?? string.Empty,
            Sex = sex ?? string.Empty,
            Keterangan = keterangan ?? string.Empty,
            Status = status,
            TipePerjalanan = tipePerjalanan,
            MetodeTarif = metodeTarif,
            PerawatId = perawatId ?? string.Empty,
            DokterId = dokterId ?? string.Empty,
            LokasiAsal = lokasiAsal ?? string.Empty,
            LokasiTujuan = lokasiTujuan ?? string.Empty,
            TglRequest = tglRequest,
            TglDispatch = tglDispatch,
            TglArrival = tglArrival,
            TglReturn = tglReturn,
            StartOdometer = startOdometer,
            EndOdometer = endOdometer,
            JaminanId = jaminanId ?? string.Empty,
            IsNonReg = isNonReg,
            IsPostedBilling = isPostedBilling,
            AlasanBatal = alasanBatal ?? string.Empty,
            BiayaBatal = biayaBatal
        };

        if (listItems != null)
        {
            model._listItems.AddRange(listItems);
        }

        return model;
    }
    #endregion

    #region CORE INVARIANT 6 (Tariff Method)
    public void SetMetodeWilayah(string tujuanTransportId, decimal konstanta)
    {
        AssertNotTerminalState();

        if (string.IsNullOrWhiteSpace(tujuanTransportId))
            throw new ArgumentException("Metode tarif Wilayah wajib menentukan TujuanTransportId.", nameof(tujuanTransportId));

        MetodeTarif = ChargingMethodEnum.Wilayah;
        TujuanTransportId = tujuanTransportId;
        Konstanta = konstanta;
    }

    public void SetMetodeJarak(decimal totalJarakKm = 0m)
    {
        AssertNotTerminalState();

        if (totalJarakKm < 0m)
            throw new ArgumentException("Total jarak tidak boleh bernilai negatif.", nameof(totalJarakKm));

        MetodeTarif = ChargingMethodEnum.Jarak;
        TujuanTransportId = string.Empty;
        Konstanta = 0m;
        if (totalJarakKm > 0m)
        {
            StartOdometer = 0m;
            EndOdometer = totalJarakKm;
        }
    }

    public void SetJarakManual(decimal totalJarakKm)
    {
        AssertNotTerminalState();

        if (totalJarakKm < 0m)
            throw new ArgumentException("Total jarak tidak boleh bernilai negatif.", nameof(totalJarakKm));

        StartOdometer = 0m;
        EndOdometer = totalJarakKm;
    }
    #endregion

    #region LIFECYCLE STATE MACHINE
    public void Dispatch(
        string ambulanceId,
        string driverId,
        string noPolisi,
        decimal startOdometer,
        string? perawatId = null,
        string? dokterId = null,
        DateTime? dispatchTime = null)
    {
        if (Status != TransportStatusEnum.Requested)
            throw new InvalidOperationException($"Tidak dapat melakukan Dispatch dari status {Status}. Transisi hanya valid dari status Requested.");

        if (string.IsNullOrWhiteSpace(ambulanceId))
            throw new ArgumentException("Ambulance ID wajib ditentukan saat dispatch.", nameof(ambulanceId));

        if (string.IsNullOrWhiteSpace(driverId))
            throw new ArgumentException("Driver ID wajib ditentukan saat dispatch.", nameof(driverId));

        if (startOdometer < 0)
            throw new ArgumentException("Start odometer tidak boleh negatif.", nameof(startOdometer));

        AmbulanceId = ambulanceId;
        DriverId = driverId;
        NoPolisi = noPolisi ?? string.Empty;
        StartOdometer = startOdometer;

        if (perawatId != null)
            PerawatId = perawatId;
        if (dokterId != null)
            DokterId = dokterId;

        TglDispatch = dispatchTime ?? DateTime.Now;
        Status = TransportStatusEnum.Dispatched;
    }

    public void RecordArrival(DateTime? arrivalTime = null)
    {
        if (Status != TransportStatusEnum.Dispatched)
            throw new InvalidOperationException($"Pencatatan tiba hanya sah saat status Dispatched. Status saat ini: {Status}.");

        TglArrival = arrivalTime ?? DateTime.Now;
    }

    public void Complete(decimal endOdometer, DateTime? returnTime = null)
    {
        if (Status != TransportStatusEnum.Dispatched)
            throw new InvalidOperationException($"Tidak dapat menyelesaikan perjalanan dari status {Status}. Transisi hanya valid dari status Dispatched.");

        if (endOdometer < StartOdometer)
            throw new ArgumentException($"End odometer ({endOdometer}) tidak boleh lebih kecil dari start odometer ({StartOdometer}).", nameof(endOdometer));

        // Core Invariant 6 Check
        if (MetodeTarif == ChargingMethodEnum.Wilayah && string.IsNullOrWhiteSpace(TujuanTransportId))
            throw new InvalidOperationException("Transaksi dengan metode Wilayah harus memiliki TujuanTransportId yang valid.");

        if (MetodeTarif == ChargingMethodEnum.Jarak && !string.IsNullOrWhiteSpace(TujuanTransportId))
            throw new InvalidOperationException("Transaksi dengan metode Jarak tidak boleh memiliki TujuanTransportId (Core Invariant 6).");

        EndOdometer = endOdometer;
        TglReturn = returnTime ?? DateTime.Now;
        Status = TransportStatusEnum.Completed;
    }

    public void Cancel(string alasanBatal, decimal biayaBatal = 0m, string petugasVoidId = "", DateTime? cancelTime = null)
    {
        if (IsPostedBilling)
            throw new InvalidOperationException("Transaksi transport yang sudah dibebankan ke Billing pasien tidak dapat dibatalkan dari operasional.");

        if (Status == TransportStatusEnum.Completed)
            throw new InvalidOperationException("Transaksi yang sudah Completed tidak dapat dibatalkan langsung. Lakukan prosedur koreksi Tata Rekening.");

        if (Status == TransportStatusEnum.Cancelled)
            throw new InvalidOperationException("Transaksi sudah dibatalkan sebelumnya.");

        if (string.IsNullOrWhiteSpace(alasanBatal))
            throw new ArgumentException("Alasan pembatalan wajib diisi.", nameof(alasanBatal));

        if (biayaBatal < 0)
            throw new ArgumentException("Biaya pembatalan (standby fee) tidak boleh bernilai negatif.", nameof(biayaBatal));

        Status = TransportStatusEnum.Cancelled;
        AlasanBatal = alasanBatal;
        BiayaBatal = biayaBatal;
        IsVoid = true;
        PetugasVoidId = petugasVoidId ?? string.Empty;
        TglVoid = cancelTime ?? DateTime.Now;
    }
    #endregion

    #region REGISTRATION & BILLING LINKING
    public void LinkRegistration(string regId)
    {
        AssertNotTerminalState();

        if (string.IsNullOrWhiteSpace(regId))
            throw new ArgumentException("RegId wajib valid untuk ditautkan.", nameof(regId));

        RegId = regId;
        IsNonReg = false;
    }

    public void SetNonRegistration(string pasienName, string alm1, string alm2 = "", string kota = "", string sex = "")
    {
        AssertNotTerminalState();

        IsNonReg = true;
        RegId = string.Empty;
        PasienName = pasienName ?? string.Empty;
        Alm1Pasien = alm1 ?? string.Empty;
        Alm2Pasien = alm2 ?? string.Empty;
        KotaPasien = kota ?? string.Empty;
        Sex = sex ?? string.Empty;
    }

    public void SetPostedBilling()
    {
        if (Status == TransportStatusEnum.Cancelled && BiayaBatal <= 0)
            throw new InvalidOperationException("Hanya transaksi aktif atau transaksi batal dengan biaya pembatalan yang dapat ditandai Posted Billing.");

        IsPostedBilling = true;
    }
    #endregion

    #region ITEM MANAGEMENT & TOTALS
    public void AddItem(TrsTransportItemModel item)
    {
        ArgumentNullException.ThrowIfNull(item);
        AssertNotTerminalState();

        item.SetTrsTransportId(TrsTransportId);
        _listItems.Add(item);
        RecalculateTotals();
    }

    public void AddItems(IEnumerable<TrsTransportItemModel> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        AssertNotTerminalState();

        foreach (var item in items)
        {
            item.SetTrsTransportId(TrsTransportId);
            _listItems.Add(item);
        }
        RecalculateTotals();
    }

    public void RemoveItem(string detilTarifId)
    {
        AssertNotTerminalState();

        var removed = _listItems.RemoveAll(x => x.DetilTarifId == detilTarifId);
        if (removed > 0)
            RecalculateTotals();
    }

    public void VoidItem(string detilTarifId)
    {
        AssertNotTerminalState();

        var item = _listItems.FirstOrDefault(x => x.DetilTarifId == detilTarifId);
        if (item != null)
        {
            item.SetVoid(true);
            RecalculateTotals();
        }
    }

    public void RecalculateTotals()
    {
        SubTotal = _listItems.Where(x => !x.IsVoid).Sum(x => x.Tarif * x.Qty);
        Diskon = _listItems.Where(x => !x.IsVoid).Sum(x => x.Diskon);
        Total = Math.Max(0m, SubTotal - Diskon);
    }
    #endregion

    private void AssertNotTerminalState()
    {
        if (Status == TransportStatusEnum.Completed || Status == TransportStatusEnum.Cancelled)
            throw new InvalidOperationException($"Mutasi transaksi tidak diizinkan pada status terminal: {Status}.");
    }
}

public interface ITrsTransportKey
{
    string TrsTransportId { get; }
}

public record TrsTransportKey(string TrsTransportId) : ITrsTransportKey;
