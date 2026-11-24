using Ardalis.GuardClauses;
using Bilreg.Domain.ApotekContext.Shared;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.ApotekContext.PenjualanFeature;

public class PenjualanModel : IPenjualanKey
{
    public const string IdPrefix = "DU";
    private static readonly HashSet<string> GeneratedIds = new();
    private static readonly object IdLock = new();

    public static string GenerateUniqueId(char editLevel = 'A')
    {
        lock (IdLock)
        {
            if (GeneratedIds.Count > 10_000)
            {
                GeneratedIds.Clear();
            }

            for (int i = 0; i < 100; i++)
            {
                var id = NunaId.NewLegacy(IdPrefix, editLevel);
                if (GeneratedIds.Add(id))
                    return id;
            }

            for (int offset = 1; offset <= 1000; offset++)
            {
                var offsetId = NunaId.NewLegacy(IdPrefix, editLevel, () => DateTime.Now.AddMilliseconds(offset * 20));
                if (GeneratedIds.Add(offsetId))
                    return offsetId;
            }

            for (char ch = 'A'; ch <= 'Z'; ch++)
            {
                var fallbackId = NunaId.NewLegacy(IdPrefix, ch);
                if (GeneratedIds.Add(fallbackId))
                    return fallbackId;
            }

            return NunaId.NewLegacy(IdPrefix, editLevel);
        }
    }

    private readonly List<PenjualanItemModel> _items;

    private PenjualanModel(
        string penjualanId,
        DateTime penjualanDate,
        string penjualanTime,
        string petugasId,
        JenisPenjualanEnum jenisPenjualan,
        PenjualanStatusEnum status,
        string resepId,
        string layananResepId,
        string dokterId,
        string layananId,
        string regId,
        string pasienName,
        string pasienAlamat,
        string pasienAlamat2,
        string pasienKota,
        string sex,
        string tipeJaminanId,
        string kelasId,
        decimal sumSubTotal,
        decimal sumBiaya,
        decimal sumTaxRupiah,
        decimal subTotal,
        decimal diskonLain,
        decimal biayaLain,
        decimal pembulatan,
        decimal bulat,
        decimal grandTotal,
        decimal kas,
        decimal bank,
        decimal kartuKredit,
        decimal sisa,
        string trsKwitansiId,
        string trsChargeId,
        string trsMutasiId,
        DateTime tglSerah,
        string jamSerah,
        string petugasSerah,
        bool isVoid,
        DateTime tglVoid,
        string jamVoid,
        string petugasVoid,
        string voidReason,
        IEnumerable<PenjualanItemModel> items,
        int version = 1)
    {
        _ = new PenjualanKey(penjualanId);

        PenjualanId = penjualanId;
        PenjualanDate = penjualanDate;
        PenjualanTime = penjualanTime ?? "00:00:00";
        PetugasId = petugasId ?? "";
        JenisPenjualan = jenisPenjualan;
        Status = status;
        ResepId = resepId ?? "";
        LayananResepId = layananResepId ?? "";
        DokterId = dokterId ?? "";
        LayananId = layananId ?? "";
        RegId = regId ?? "";
        PasienName = pasienName ?? "";
        PasienAlamat = pasienAlamat ?? "";
        PasienAlamat2 = pasienAlamat2 ?? "";
        PasienKota = pasienKota ?? "";
        Sex = sex ?? "";
        TipeJaminanId = tipeJaminanId ?? "";
        KelasId = kelasId ?? "";

        SumSubTotal = sumSubTotal;
        SumBiaya = sumBiaya;
        SumTaxRupiah = sumTaxRupiah;
        SubTotal = subTotal;
        DiskonLain = diskonLain;
        BiayaLain = biayaLain;
        Pembulatan = pembulatan;
        Bulat = bulat;
        GrandTotal = grandTotal;
        Kas = kas;
        Bank = bank;
        KartuKredit = kartuKredit;
        Sisa = sisa;
        TrsKwitansiId = trsKwitansiId ?? "";
        TrsChargeId = trsChargeId ?? "";

        TrsMutasiId = trsMutasiId ?? "";
        TglSerah = tglSerah;
        JamSerah = jamSerah ?? "00:00:00";
        PetugasSerah = petugasSerah ?? "";

        IsVoid = isVoid;
        TglVoid = tglVoid;
        JamVoid = jamVoid ?? "00:00:00";
        PetugasVoid = petugasVoid ?? "";
        VoidReason = voidReason ?? "";

        _items = items.ToList();
        Version = version;
    }

    public static IPenjualanKey Key(string penjualanId) => new PenjualanKey(penjualanId);

    public static PenjualanModel CreateFromTelaah(
        string resepId,
        string regId,
        string pasienName,
        string dokterId,
        string layananId,
        string layananResepId,
        string tipeJaminanId,
        string kelasId,
        string petugasId,
        DateTime penjualanDate,
        IEnumerable<PenjualanItemModel> items,
        string pasienAlamat = "",
        string pasienAlamat2 = "",
        string pasienKota = "",
        string sex = "",
        decimal diskonLain = 0,
        decimal biayaLain = 0,
        decimal pembulatan = 0,
        decimal bulat = 0,
        string? penjualanId = null)
    {
        Guard.Against.NullOrWhiteSpace(resepId, nameof(resepId));
        Guard.Against.NullOrWhiteSpace(regId, nameof(regId));
        Guard.Against.NullOrWhiteSpace(pasienName, nameof(pasienName));
        Guard.Against.NullOrWhiteSpace(dokterId, nameof(dokterId));
        Guard.Against.NullOrWhiteSpace(layananId, nameof(layananId));
        Guard.Against.NullOrWhiteSpace(petugasId, nameof(petugasId));

        var itemList = items.ToList();
        if (itemList.Count == 0)
            throw new ApotekDomainException("Penjualan resep wajib memiliki minimal satu baris item.");

        var id = !string.IsNullOrWhiteSpace(penjualanId) ? penjualanId : GenerateUniqueId();

        var model = new PenjualanModel(
            id,
            penjualanDate,
            penjualanDate.ToString("HH:mm:ss"),
            petugasId,
            JenisPenjualanEnum.PrescriptionBased,
            PenjualanStatusEnum.Issued,
            resepId,
            layananResepId,
            dokterId,
            layananId,
            regId,
            pasienName,
            pasienAlamat,
            pasienAlamat2,
            pasienKota,
            sex,
            tipeJaminanId,
            kelasId,
            sumSubTotal: 0,
            sumBiaya: 0,
            sumTaxRupiah: 0,
            subTotal: 0,
            diskonLain,
            biayaLain,
            pembulatan,
            bulat,
            grandTotal: 0,
            kas: 0,
            bank: 0,
            kartuKredit: 0,
            sisa: 0,
            trsKwitansiId: "",
            trsChargeId: "",
            trsMutasiId: "",
            tglSerah: ApotekDate.Empty,
            jamSerah: "00:00:00",
            petugasSerah: "",
            isVoid: false,
            tglVoid: ApotekDate.Empty,
            jamVoid: "00:00:00",
            petugasVoid: "",
            voidReason: "",
            itemList,
            version: 1);

        model.RecalculateTotals();
        return model;
    }

    public static PenjualanModel CreateDirect(
        string regId,
        string pasienName,
        string dokterId,
        string layananId,
        string tipeJaminanId,
        string kelasId,
        string petugasId,
        DateTime penjualanDate,
        IEnumerable<PenjualanItemModel> items,
        string layananResepId = "",
        string pasienAlamat = "",
        string pasienAlamat2 = "",
        string pasienKota = "",
        string sex = "",
        decimal diskonLain = 0,
        decimal biayaLain = 0,
        decimal pembulatan = 0,
        decimal bulat = 0,
        string? penjualanId = null)
    {
        Guard.Against.NullOrWhiteSpace(regId, nameof(regId));
        Guard.Against.NullOrWhiteSpace(pasienName, nameof(pasienName));
        Guard.Against.NullOrWhiteSpace(dokterId, nameof(dokterId));
        Guard.Against.NullOrWhiteSpace(layananId, nameof(layananId));
        Guard.Against.NullOrWhiteSpace(petugasId, nameof(petugasId));

        var itemList = items.ToList();
        if (itemList.Count == 0)
            throw new ApotekDomainException("Penjualan langsung registrasi wajib memiliki minimal satu baris item.");

        var id = !string.IsNullOrWhiteSpace(penjualanId) ? penjualanId : GenerateUniqueId();

        var model = new PenjualanModel(
            id,
            penjualanDate,
            penjualanDate.ToString("HH:mm:ss"),
            petugasId,
            JenisPenjualanEnum.RegisteredDirect,
            PenjualanStatusEnum.Issued,
            resepId: "",
            layananResepId,
            dokterId,
            layananId,
            regId,
            pasienName,
            pasienAlamat,
            pasienAlamat2,
            pasienKota,
            sex,
            tipeJaminanId,
            kelasId,
            sumSubTotal: 0,
            sumBiaya: 0,
            sumTaxRupiah: 0,
            subTotal: 0,
            diskonLain,
            biayaLain,
            pembulatan,
            bulat,
            grandTotal: 0,
            kas: 0,
            bank: 0,
            kartuKredit: 0,
            sisa: 0,
            trsKwitansiId: "",
            trsChargeId: "",
            trsMutasiId: "",
            tglSerah: ApotekDate.Empty,
            jamSerah: "00:00:00",
            petugasSerah: "",
            isVoid: false,
            tglVoid: ApotekDate.Empty,
            jamVoid: "00:00:00",
            petugasVoid: "",
            voidReason: "",
            itemList,
            version: 1);

        model.RecalculateTotals();
        return model;
    }

    public static PenjualanModel CreateWalkIn(
        string pembeliName,
        string pembeliAlamat,
        string layananId,
        string petugasId,
        DateTime penjualanDate,
        IEnumerable<PenjualanItemModel> items,
        string pembeliKota = "",
        string sex = "",
        decimal diskonLain = 0,
        decimal biayaLain = 0,
        decimal pembulatan = 0,
        decimal bulat = 0,
        string? penjualanId = null)
    {
        Guard.Against.NullOrWhiteSpace(pembeliName, nameof(pembeliName));
        Guard.Against.NullOrWhiteSpace(layananId, nameof(layananId));
        Guard.Against.NullOrWhiteSpace(petugasId, nameof(petugasId));

        var itemList = items.ToList();
        if (itemList.Count == 0)
            throw new ApotekDomainException("Penjualan walk-in bebas wajib memiliki minimal satu baris item.");

        foreach (var item in itemList)
        {
            item.MarkBeliBebas();
        }

        var id = !string.IsNullOrWhiteSpace(penjualanId) ? penjualanId : GenerateUniqueId();

        var model = new PenjualanModel(
            id,
            penjualanDate,
            penjualanDate.ToString("HH:mm:ss"),
            petugasId,
            JenisPenjualanEnum.WalkInOTC,
            PenjualanStatusEnum.Issued,
            resepId: "",
            layananResepId: "",
            dokterId: "",
            layananId,
            regId: "",
            pasienName: pembeliName,
            pasienAlamat: pembeliAlamat ?? "",
            pasienAlamat2: "",
            pasienKota: pembeliKota ?? "",
            sex: sex ?? "",
            tipeJaminanId: "00000",
            kelasId: "",
            sumSubTotal: 0,
            sumBiaya: 0,
            sumTaxRupiah: 0,
            subTotal: 0,
            diskonLain,
            biayaLain,
            pembulatan,
            bulat,
            grandTotal: 0,
            kas: 0,
            bank: 0,
            kartuKredit: 0,
            sisa: 0,
            trsKwitansiId: "",
            trsChargeId: "",
            trsMutasiId: "",
            tglSerah: ApotekDate.Empty,
            jamSerah: "00:00:00",
            petugasSerah: "",
            isVoid: false,
            tglVoid: ApotekDate.Empty,
            jamVoid: "00:00:00",
            petugasVoid: "",
            voidReason: "",
            itemList,
            version: 1);

        model.RecalculateTotals();
        return model;
    }

    public static PenjualanModel Rehydrate(
        string penjualanId,
        DateTime penjualanDate,
        string penjualanTime,
        string petugasId,
        JenisPenjualanEnum jenisPenjualan,
        PenjualanStatusEnum status,
        string resepId,
        string layananResepId,
        string dokterId,
        string layananId,
        string regId,
        string pasienName,
        string pasienAlamat,
        string pasienAlamat2,
        string pasienKota,
        string sex,
        string tipeJaminanId,
        string kelasId,
        decimal sumSubTotal,
        decimal sumBiaya,
        decimal sumTaxRupiah,
        decimal subTotal,
        decimal diskonLain,
        decimal biayaLain,
        decimal pembulatan,
        decimal bulat,
        decimal grandTotal,
        decimal kas,
        decimal bank,
        decimal kartuKredit,
        decimal sisa,
        string trsKwitansiId,
        string trsChargeId,
        string trsMutasiId,
        DateTime tglSerah,
        string jamSerah,
        string petugasSerah,
        bool isVoid,
        DateTime tglVoid,
        string jamVoid,
        string petugasVoid,
        string voidReason,
        IEnumerable<PenjualanItemModel> items,
        int version = 1)
    {
        return new PenjualanModel(
            penjualanId,
            penjualanDate,
            penjualanTime,
            petugasId,
            jenisPenjualan,
            status,
            resepId,
            layananResepId,
            dokterId,
            layananId,
            regId,
            pasienName,
            pasienAlamat,
            pasienAlamat2,
            pasienKota,
            sex,
            tipeJaminanId,
            kelasId,
            sumSubTotal,
            sumBiaya,
            sumTaxRupiah,
            subTotal,
            diskonLain,
            biayaLain,
            pembulatan,
            bulat,
            grandTotal,
            kas,
            bank,
            kartuKredit,
            sisa,
            trsKwitansiId,
            trsChargeId,
            trsMutasiId,
            tglSerah,
            jamSerah,
            petugasSerah,
            isVoid,
            tglVoid,
            jamVoid,
            petugasVoid,
            voidReason,
            items,
            version);
    }

    public void Void(string actorId, DateTime voidAt, string reason, bool tataRekeningAllows)
    {
        Guard.Against.NullOrWhiteSpace(actorId, nameof(actorId));
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));

        if (IsVoid || Status == PenjualanStatusEnum.Cancelled)
            throw new ApotekDomainException($"Transaksi penjualan '{PenjualanId}' sudah dibatalkan sebelumnya.");

        if (!tataRekeningAllows)
            throw new ApotekDomainException($"Pembatalan penjualan '{PenjualanId}' ditolak oleh Tata Rekening sesuai aturan ADR-APT-003.");

        Status = PenjualanStatusEnum.Cancelled;
        IsVoid = true;
        TglVoid = voidAt;
        JamVoid = voidAt.ToString("HH:mm:ss");
        PetugasVoid = actorId;
        VoidReason = reason;

        foreach (var item in _items)
        {
            item.Void();
        }

        RecalculateTotals();
    }

    public void UpdateHandoverInfo(DateTime tglSerah, string jamSerah, string petugasSerah, string trsMutasiId)
    {
        if (IsVoid || Status == PenjualanStatusEnum.Cancelled)
            throw new ApotekDomainException($"Tidak dapat melakukan penyerahan obat untuk transaksi '{PenjualanId}' yang telah dibatalkan.");

        Guard.Against.NullOrWhiteSpace(petugasSerah, nameof(petugasSerah));
        Guard.Against.NullOrWhiteSpace(trsMutasiId, nameof(trsMutasiId));

        TglSerah = tglSerah;
        JamSerah = string.IsNullOrWhiteSpace(jamSerah) ? tglSerah.ToString("HH:mm:ss") : jamSerah;
        PetugasSerah = petugasSerah;
        TrsMutasiId = trsMutasiId;
    }

    public void SetChargeId(string trsChargeId)
    {
        if (IsVoid || Status == PenjualanStatusEnum.Cancelled)
            throw new ApotekDomainException("Tidak dapat mencatat charge pada transaksi yang telah dibatalkan.");

        TrsChargeId = trsChargeId ?? "";
    }

    public void SetPaymentClearance(string kwitansiId, decimal kas, decimal bank, decimal kartuKredit)
    {
        if (IsVoid || Status == PenjualanStatusEnum.Cancelled)
            throw new ApotekDomainException("Tidak dapat menyelesaikan pembayaran transaksi yang telah dibatalkan.");

        TrsKwitansiId = kwitansiId ?? "";
        Kas = kas;
        Bank = bank;
        KartuKredit = kartuKredit;
        Status = PenjualanStatusEnum.FinanciallyCleared;
        RecalculateTotals();
    }

    public void RecalculateTotals()
    {
        var activeItems = _items.Where(x => !x.IsVoid).ToList();
        SumSubTotal = activeItems.Sum(x => x.SubTotal);
        SumBiaya = activeItems.Sum(x => x.Biaya);
        SumTaxRupiah = activeItems.Sum(x => x.TaxRupiah);
        var sumFee = activeItems.Sum(x => x.Fee);

        SubTotal = SumSubTotal + SumBiaya + SumTaxRupiah + sumFee;
        GrandTotal = SubTotal - DiskonLain + BiayaLain + Pembulatan + Bulat;
        if (GrandTotal < 0) GrandTotal = 0;
        Sisa = GrandTotal - Kas - Bank - KartuKredit;
    }

    public string PenjualanId { get; }
    public DateTime PenjualanDate { get; }
    public string PenjualanTime { get; }
    public string PetugasId { get; }
    public JenisPenjualanEnum JenisPenjualan { get; }
    public PenjualanStatusEnum Status { get; private set; }
    public string ResepId { get; }
    public string LayananResepId { get; }
    public string DokterId { get; }
    public string LayananId { get; }
    public string RegId { get; }
    public string PasienName { get; }
    public string PasienAlamat { get; }
    public string PasienAlamat2 { get; }
    public string PasienKota { get; }
    public string Sex { get; }
    public string TipeJaminanId { get; }
    public string KelasId { get; }

    public decimal SumSubTotal { get; private set; }
    public decimal SumBiaya { get; private set; }
    public decimal SumTaxRupiah { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal DiskonLain { get; private set; }
    public decimal BiayaLain { get; private set; }
    public decimal Pembulatan { get; private set; }
    public decimal Bulat { get; private set; }
    public decimal GrandTotal { get; private set; }
    public decimal Kas { get; private set; }
    public decimal Bank { get; private set; }
    public decimal KartuKredit { get; private set; }
    public decimal Sisa { get; private set; }

    public string TrsKwitansiId { get; private set; }
    public string TrsChargeId { get; private set; }
    public string TrsMutasiId { get; private set; }
    public DateTime TglSerah { get; private set; }
    public string JamSerah { get; private set; }
    public string PetugasSerah { get; private set; }

    public bool IsVoid { get; private set; }
    public DateTime TglVoid { get; private set; }
    public string JamVoid { get; private set; }
    public string PetugasVoid { get; private set; }
    public string VoidReason { get; private set; }

    public IReadOnlyList<PenjualanItemModel> Items => _items.AsReadOnly();
    public int Version { get; private set; }
}
