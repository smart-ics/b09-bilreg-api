using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Application.ApotekContext.TelaahResepFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Application.SalesContext.ResepFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ApotekContext.PenjualanFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using MediatR;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.ApotekContext.PenjualanFeature.UseCases;

#region Command DTOs

/// <summary>
/// Command penerbitan tagihan farmasi Jenis 1 berbasis E-Resep yang telah disetujui pada tahapan telaah resep.
/// Mendukung pemisahan otomatis tagihan multi-penjamin BPJS (Fornas vs Non-Fornas) menjadi 2 dokumen DU terpisah.
/// </summary>
public record PenjualanCreateFromTelaahCmd(
    string UserId,
    string ResepId,
    string? LayananId = null,
    string? TipeJaminanId = null,
    string? KelasId = null,
    DateTime? TransactionDate = null,
    bool? IsRanap = null,
    IReadOnlyList<string>? NonFornasBrgIds = null
) : IRequest<IReadOnlyList<string>>;

/// <summary>
/// Input baris item untuk penjualan langsung (Jenis 2).
/// </summary>
public record PenjualanDirectItemInput(
    string BrgId,
    string BrgName,
    string SatuanId,
    string SatuanName,
    decimal Qty,
    decimal? CustomHargaSatuan = null,
    decimal? CustomDiskon = null,
    decimal? CustomBiaya = null,
    decimal? CustomTaxProsen = null,
    decimal? CustomFee = null,
    string Etiket = "",
    decimal EtiketQty = 0,
    decimal EtiketHari = 0,
    string EtiketKdPakai = "",
    string EtiketCatatan = "",
    bool IsRacik = false,
    bool IsKomponen = false,
    string RacikId = "");

/// <summary>
/// Command penerbitan tagihan farmasi Jenis 2 terikat nomor registrasi aktif tanpa e-resep.
/// </summary>
public record PenjualanCreateDirectCmd(
    string UserId,
    string RegId,
    string DokterId,
    string LayananId,
    IReadOnlyList<PenjualanDirectItemInput> Items,
    string? TipeJaminanId = null,
    string? KelasId = null,
    DateTime? TransactionDate = null,
    string LayananResepId = "",
    decimal DiskonLain = 0,
    decimal BiayaLain = 0,
    decimal Pembulatan = 0,
    bool? IsRanap = null,
    string? PenjualanId = null
) : IRequest<string>;

/// <summary>
/// Input baris item untuk penjualan bebas walk-in OTC (Jenis 3).
/// </summary>
public record PenjualanWalkInItemInput(
    string BrgId,
    string BrgName,
    string SatuanId,
    string SatuanName,
    decimal Qty,
    decimal? CustomHargaSatuan = null,
    decimal? CustomDiskon = null,
    decimal? CustomBiaya = null,
    decimal? CustomTaxProsen = null,
    decimal? CustomFee = null,
    string Etiket = "",
    decimal EtiketQty = 0,
    decimal EtiketHari = 0,
    string EtiketKdPakai = "",
    string EtiketCatatan = "",
    bool IsRacik = false,
    bool IsKomponen = false,
    string RacikId = "");

/// <summary>
/// Command penerbitan tagihan farmasi Jenis 3 (Walk-in OTC) tanpa registrasi dan resep.
/// </summary>
public record PenjualanCreateWalkInCmd(
    string UserId,
    string PembeliName,
    string LayananId,
    IReadOnlyList<PenjualanWalkInItemInput> Items,
    string PembeliAlamat = "",
    string PembeliKota = "",
    string Sex = "",
    DateTime? TransactionDate = null,
    decimal DiskonLain = 0,
    decimal BiayaLain = 0,
    decimal Pembulatan = 0,
    string? PenjualanId = null
) : IRequest<string>;

/// <summary>
/// Command pembatalan tagihan farmasi (Soft-Void) dengan validasi mutabilitas ADR-APT-003.
/// </summary>
public record PenjualanVoidCmd(
    string UserId,
    string PenjualanId,
    string Reason,
    DateTime? VoidAt = null
) : IRequest<Unit>;

/// <summary>
/// Command internal hook pembaruan data serah terima fisik obat dari modul Dispensing.
/// </summary>
public record PenjualanUpdateHandoverInfoCmd(
    string UserId,
    string PenjualanId,
    DateTime TglSerah,
    string JamSerah,
    string PetugasSerah,
    string TrsMutasiId
) : IRequest<Unit>;

#endregion

#region Command Handlers

/// <summary>
/// Handler pembentukan tagihan Jenis 1 berbasis telaah resep dengan automated BPJS mixed-payer split.
/// </summary>
public class PenjualanCreateFromTelaahHandler : IRequestHandler<PenjualanCreateFromTelaahCmd, IReadOnlyList<string>>
{
    private readonly IPenjualanRepo _penjualanRepo;
    private readonly ITelaahResepRepo _telaahRepo;
    private readonly IResepRepo _resepRepo;
    private readonly IRegRepo _regRepo;
    private readonly ITrkTarifPricingPort _pricingPort;
    private readonly ITrkBillingChargePort _billingChargePort;
    private readonly IAptAuthorizationPolicy _auth;
    private readonly IFornasCheckPort _fornasCheckPort;
    private readonly IPasienRepo? _pasienRepo;

    public PenjualanCreateFromTelaahHandler(
        IPenjualanRepo penjualanRepo,
        ITelaahResepRepo telaahRepo,
        IResepRepo resepRepo,
        IRegRepo regRepo,
        ITrkTarifPricingPort pricingPort,
        ITrkBillingChargePort billingChargePort,
        IAptAuthorizationPolicy auth,
        IFornasCheckPort? fornasCheckPort = null,
        IPasienRepo? pasienRepo = null)
    {
        _penjualanRepo = penjualanRepo;
        _telaahRepo = telaahRepo;
        _resepRepo = resepRepo;
        _regRepo = regRepo;
        _pricingPort = pricingPort;
        _billingChargePort = billingChargePort;
        _auth = auth;
        _fornasCheckPort = fornasCheckPort ?? new DefaultFornasCheckPort();
        _pasienRepo = pasienRepo;
    }

    public Task<IReadOnlyList<string>> Handle(PenjualanCreateFromTelaahCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(PenjualanCreateFromTelaahCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.ResepId, nameof(request.ResepId));

        // Idempotency: Jika tagihan aktif sudah terbit untuk resep ini, kembalikan daftar ID penjualan eksis
        var existingSales = _penjualanRepo.ListByResep(request.ResepId)
            .Where(x => !x.IsVoid)
            .ToList();
        if (existingSales.Count > 0)
        {
            return Task.FromResult<IReadOnlyList<string>>(existingSales.Select(x => x.PenjualanId).ToList());
        }

        // 1. Memuat telaah resep
        var telaah = _telaahRepo.LoadByResep(request.ResepId)
            .GetValueOrThrow($"Dokumen Telaah Resep untuk '{request.ResepId}' tidak ditemukan.");

        // 2. Memvalidasi kelolosan telaah klinis
        if (telaah.TelaahStatus != TelaahStatusEnum.Approved && telaah.TelaahStatus != TelaahStatusEnum.PartiallyApproved)
        {
            throw new ApotekDomainException(
                $"Resep '{request.ResepId}' tidak dapat diproses penjualan karena telaah belum Approved atau PartiallyApproved (Status saat ini: {telaah.TelaahStatus}).");
        }

        // 3. Membaca AcceptedItems
        var acceptedItems = telaah.AcceptedItems();
        if (acceptedItems.Count == 0)
        {
            throw new ApotekDomainException($"Resep '{request.ResepId}' tidak memiliki item obat yang disetujui (Accepted).");
        }

        // 4. Memuat Resep dan Registrasi
        var resep = _resepRepo.LoadEntity(ResepModel.Key(request.ResepId))
            .GetValueOrThrow($"Resep '{request.ResepId}' tidak ditemukan.");

        var regId = !string.IsNullOrWhiteSpace(resep.Register.RegId) ? resep.Register.RegId : telaah.RegId;
        var regOpt = _regRepo.LoadEntity(RegModel.Key(regId));
        var reg = regOpt.HasValue ? regOpt.Value : null;

        var pasienName = !string.IsNullOrWhiteSpace(resep.Register.PasienName)
            ? resep.Register.PasienName
            : (reg?.Pasien?.PasienName ?? "Pasien");

        var pasienAlamat = "";
        var pasienAlamat2 = "";
        var pasienKota = "";
        var sex = "";

        if (_pasienRepo != null && !string.IsNullOrWhiteSpace(resep.Register.PasienId))
        {
            var pOpt = _pasienRepo.LoadEntity(PasienModel.Key(resep.Register.PasienId));
            if (pOpt.HasValue)
            {
                var p = pOpt.Value;
                var alamatArr = p.Person?.Alamat?.Alamat;
                pasienAlamat = (alamatArr != null && alamatArr.Length > 0) ? alamatArr[0] : "";
                pasienAlamat2 = (alamatArr != null && alamatArr.Length > 1) ? alamatArr[1] : "";
                pasienKota = p.Person?.Alamat?.Kota ?? "";
                sex = p.Person?.Gender ?? "";
            }
        }

        var dokterId = !string.IsNullOrWhiteSpace(resep.Dokter.DokterId) && resep.Dokter.DokterId != "-"
            ? resep.Dokter.DokterId
            : (reg?.Dokter?.PpaId ?? "DOK01");

        var layananId = !string.IsNullOrWhiteSpace(request.LayananId)
            ? request.LayananId
            : (!string.IsNullOrWhiteSpace(resep.LayananStok.LayananId) && resep.LayananStok.LayananId != "-"
                ? resep.LayananStok.LayananId
                : (!string.IsNullOrWhiteSpace(resep.Layanan.LayananId) && resep.Layanan.LayananId != "-"
                    ? resep.Layanan.LayananId
                    : "APT01"));

        var tipeJaminanId = !string.IsNullOrWhiteSpace(request.TipeJaminanId)
            ? request.TipeJaminanId
            : (!string.IsNullOrWhiteSpace(reg?.TipeJaminan?.TipeJaminanId)
                ? reg.TipeJaminan.TipeJaminanId
                : "00000");

        var kelasId = !string.IsNullOrWhiteSpace(request.KelasId)
            ? request.KelasId
            : (!string.IsNullOrWhiteSpace(reg?.Kelas?.KelasId)
                ? reg.Kelas.KelasId
                : "3");

        var isRanap = request.IsRanap ?? (reg != null && reg.JenisReg == JenisRegEnum.RegInap);
        var trsDate = request.TransactionDate ?? DateTime.Now;

        // 5. Evaluasi Pemisahan Multi-Payer (Mixed-Payer Split)
        bool isBpjs = string.Equals(tipeJaminanId, "JKN00", StringComparison.OrdinalIgnoreCase);
        var fornasItems = new List<TelaahResepItemModel>();
        var nonFornasItems = new List<TelaahResepItemModel>();

        if (isBpjs)
        {
            foreach (var item in acceptedItems)
            {
                bool isNonFornas = false;
                if (request.NonFornasBrgIds != null && request.NonFornasBrgIds.Contains(item.AcceptedBrgId))
                {
                    isNonFornas = true;
                }
                else if (!_fornasCheckPort.IsFornas(regId, item.AcceptedBrgId))
                {
                    isNonFornas = true;
                }

                if (isNonFornas)
                    nonFornasItems.Add(item);
                else
                    fornasItems.Add(item);
            }
        }

        var createdIds = new List<string>();

        if (isBpjs && fornasItems.Count > 0 && nonFornasItems.Count > 0)
        {
            // Pemisahan otomatis menjadi 2 DU dokumen terpisah
            // 1. Dokumen DU BPJS (Fornas)
            var bpjsModel = CreateDUFromItems(
                request.ResepId, regId, pasienName, pasienAlamat, pasienAlamat2, pasienKota, sex,
                dokterId, layananId, resep.Layanan.LayananId, "JKN00", kelasId, request.UserId, trsDate,
                fornasItems, isBpjsDoc: true, resep, editLevel: 'A');

            // 2. Dokumen DU Pasien Umum Mandiri (Non-Fornas)
            var mandiriModel = CreateDUFromItems(
                request.ResepId, regId, pasienName, pasienAlamat, pasienAlamat2, pasienKota, sex,
                dokterId, layananId, resep.Layanan.LayananId, "00000", kelasId, request.UserId, trsDate,
                nonFornasItems, isBpjsDoc: false, resep, editLevel: 'B');

            SaveAndCharge(bpjsModel, regId, layananId, kelasId, request.UserId, trsDate, isRanap, request.ResepId);
            SaveAndCharge(mandiriModel, regId, layananId, kelasId, request.UserId, trsDate, isRanap, request.ResepId);

            createdIds.Add(bpjsModel.PenjualanId);
            createdIds.Add(mandiriModel.PenjualanId);
        }
        else
        {
            var targetTipeJaminan = isBpjs ? (fornasItems.Count > 0 ? "JKN00" : "00000") : tipeJaminanId;
            var isBpjsDoc = string.Equals(targetTipeJaminan, "JKN00", StringComparison.OrdinalIgnoreCase);
            var model = CreateDUFromItems(
                request.ResepId, regId, pasienName, pasienAlamat, pasienAlamat2, pasienKota, sex,
                dokterId, layananId, resep.Layanan.LayananId, targetTipeJaminan, kelasId, request.UserId, trsDate,
                acceptedItems, isBpjsDoc: isBpjsDoc, resep, editLevel: 'A');

            SaveAndCharge(model, regId, layananId, kelasId, request.UserId, trsDate, isRanap, request.ResepId);
            createdIds.Add(model.PenjualanId);
        }

        return Task.FromResult<IReadOnlyList<string>>(createdIds);
    }

    private PenjualanModel CreateDUFromItems(
        string resepId,
        string regId,
        string pasienName,
        string pasienAlamat,
        string pasienAlamat2,
        string pasienKota,
        string sex,
        string dokterId,
        string layananId,
        string layananResepId,
        string tipeJaminanId,
        string kelasId,
        string petugasId,
        DateTime trsDate,
        IReadOnlyList<TelaahResepItemModel> items,
        bool isBpjsDoc,
        ResepModel resep,
        char editLevel = 'A')
    {
        var penjualanId = PenjualanModel.GenerateUniqueId(editLevel);
        var domainItems = new List<PenjualanItemModel>();
        int noUrut = 1;

        foreach (var telaahItem in items)
        {
            var origObat = resep.ListObat.FirstOrDefault(x => x.NoUrut == telaahItem.ResepItemNo)
                ?? resep.ListObat.FirstOrDefault(x => x.Brg.BrgId == telaahItem.PrescribedBrgId);

            var satuanId = origObat?.Satuan?.SatuanId ?? "TAB";
            var satuanName = origObat?.Satuan?.SatuanName ?? "Tablet";
            var isRacik = origObat?.IsRacik ?? false;
            var etiket = origObat?.Etiket?.Signa ?? "";
            var etiketCatatan = origObat?.Etiket?.Note ?? "";

            var pricing = _pricingPort.CalculateItemPrice(new TrkTarifItemPricingRequest(
                BrgId: telaahItem.AcceptedBrgId,
                TipeJaminanId: tipeJaminanId,
                TransactionDate: trsDate,
                Qty: telaahItem.AcceptedQty,
                SatuanId: satuanId));

            var hargaSatuan = pricing.HargaSatuan;
            var diskon = Math.Round(telaahItem.AcceptedQty * pricing.HargaSatuan * pricing.DiskonPersen / 100m, 2);
            var biaya = pricing.BiayaPerObat_r;
            var fee = isRacik ? pricing.BiayaPerResep_R : 0m;
            var subTotal = (telaahItem.AcceptedQty * hargaSatuan) - diskon;
            var total = subTotal + biaya + fee;
            var nilaiKlaim = isBpjsDoc ? total : 0m;

            var itemId = $"{penjualanId}{noUrut:D3}";
            var lineItem = PenjualanItemModel.Create(
                noUrut: noUrut++,
                penjualanItemId: itemId,
                brgId: telaahItem.AcceptedBrgId,
                brgName: telaahItem.AcceptedBrgName,
                satuanId: satuanId,
                satuanName: satuanName,
                qty: telaahItem.AcceptedQty,
                hargaSatuan: hargaSatuan,
                diskon: diskon,
                biaya: biaya,
                taxProsen: 0m,
                taxRupiah: 0m,
                fee: fee,
                etiket: etiket,
                etiketQty: origObat?.Etiket?.UnitDose ?? 0,
                etiketHari: origObat?.Etiket?.Frequency ?? 0,
                etiketKdPakai: origObat?.Etiket?.MealRelation ?? "",
                etiketCatatan: etiketCatatan,
                isRacik: isRacik,
                isKomponen: false,
                racikId: "",
                isBeliBebas: false,
                nilaiKlaim: nilaiKlaim);

            domainItems.Add(lineItem);

            // Komponen racikan jika ada
            if (isRacik && origObat?.ListItemRacik != null)
            {
                foreach (var komp in origObat.ListItemRacik)
                {
                    var kompPricing = _pricingPort.CalculateItemPrice(new TrkTarifItemPricingRequest(
                        BrgId: komp.Brg.BrgId,
                        TipeJaminanId: tipeJaminanId,
                        TransactionDate: trsDate,
                        Qty: komp.Qty,
                        SatuanId: komp.Satuan?.SatuanId ?? "TAB"));

                    var kompSubTotal = komp.Qty * kompPricing.HargaSatuan;
                    var kompTotal = kompSubTotal;
                    var kompItemId = $"{penjualanId}{noUrut:D3}";

                    var kompLineItem = PenjualanItemModel.Create(
                        noUrut: noUrut++,
                        penjualanItemId: kompItemId,
                        brgId: komp.Brg.BrgId,
                        brgName: komp.Brg.BrgName,
                        satuanId: komp.Satuan?.SatuanId ?? "TAB",
                        satuanName: komp.Satuan?.SatuanName ?? "Tablet",
                        qty: komp.Qty,
                        hargaSatuan: kompPricing.HargaSatuan,
                        diskon: 0m,
                        biaya: 0m,
                        taxProsen: 0m,
                        taxRupiah: 0m,
                        fee: 0m,
                        etiket: "",
                        etiketQty: 0,
                        etiketHari: 0,
                        etiketKdPakai: "",
                        etiketCatatan: "",
                        isRacik: false,
                        isKomponen: true,
                        racikId: itemId,
                        isBeliBebas: false,
                        nilaiKlaim: isBpjsDoc ? kompTotal : 0m);

                    domainItems.Add(kompLineItem);
                }
            }
        }

        var rounding = _pricingPort.CalculateRounding(domainItems.Sum(x => x.Total));

        var model = PenjualanModel.CreateFromTelaah(
            resepId: resepId,
            regId: regId,
            pasienName: pasienName,
            dokterId: dokterId,
            layananId: layananId,
            tipeJaminanId: tipeJaminanId,
            kelasId: kelasId,
            petugasId: petugasId,
            penjualanDate: trsDate,
            items: domainItems,
            layananResepId: layananResepId,
            pasienAlamat: pasienAlamat,
            pasienAlamat2: pasienAlamat2,
            pasienKota: pasienKota,
            sex: sex,
            pembulatan: rounding.RoundingDifference,
            penjualanId: penjualanId);

        return model;
    }

    private void SaveAndCharge(
        PenjualanModel model,
        string regId,
        string layananId,
        string kelasId,
        string userId,
        DateTime trsDate,
        bool isRanap,
        string resepId)
    {
        if (isRanap)
        {
            var chargeReq = new TrkBillingChargeRequest(
                PenjualanId: model.PenjualanId,
                RegId: regId,
                LayananId: layananId,
                KelasId: kelasId,
                PetugasId: userId,
                TransactionDate: trsDate,
                SubTotal: model.SubTotal,
                Diskon: model.DiskonLain,
                Biaya: model.BiayaLain,
                Tax: model.SumTaxRupiah,
                Total: model.GrandTotal,
                Keterangan: $"Penjualan Farmasi DU ({model.PenjualanId}) Resep {resepId}");

            var chargeId = _billingChargePort.PostBillingCharge(chargeReq);
            model.SetChargeId(chargeId);
        }

        _penjualanRepo.SaveChanges(model);
    }
}

/// <summary>
/// Handler pembentukan tagihan Jenis 2 (Penjualan Langsung Registrasi).
/// </summary>
public class PenjualanCreateDirectHandler : IRequestHandler<PenjualanCreateDirectCmd, string>
{
    private readonly IPenjualanRepo _penjualanRepo;
    private readonly IRegRepo _regRepo;
    private readonly ITrkTarifPricingPort _pricingPort;
    private readonly ITrkBillingChargePort _billingChargePort;
    private readonly IAptAuthorizationPolicy _auth;
    private readonly IPasienRepo? _pasienRepo;

    public PenjualanCreateDirectHandler(
        IPenjualanRepo penjualanRepo,
        IRegRepo regRepo,
        ITrkTarifPricingPort pricingPort,
        ITrkBillingChargePort billingChargePort,
        IAptAuthorizationPolicy auth,
        IPasienRepo? pasienRepo = null)
    {
        _penjualanRepo = penjualanRepo;
        _regRepo = regRepo;
        _pricingPort = pricingPort;
        _billingChargePort = billingChargePort;
        _auth = auth;
        _pasienRepo = pasienRepo;
    }

    public Task<string> Handle(PenjualanCreateDirectCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(PenjualanCreateDirectCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));
        Guard.Against.NullOrWhiteSpace(request.DokterId, nameof(request.DokterId));
        Guard.Against.NullOrWhiteSpace(request.LayananId, nameof(request.LayananId));

        if (request.Items == null || request.Items.Count == 0)
            throw new ApotekDomainException("Penjualan langsung registrasi wajib memiliki minimal satu baris item.");

        // Validasi keabsahan registrasi pasien aktif
        var regOpt = _regRepo.LoadEntity(RegModel.Key(request.RegId));
        if (!regOpt.HasValue)
            throw new KeyNotFoundException($"Registrasi '{request.RegId}' tidak ditemukan.");

        var reg = regOpt.Value;
        if (!reg.IsAktif)
            throw new ApotekDomainException($"Registrasi '{request.RegId}' tidak aktif atau sudah keluar/batal.");

        var tipeJaminanId = !string.IsNullOrWhiteSpace(request.TipeJaminanId)
            ? request.TipeJaminanId
            : (!string.IsNullOrWhiteSpace(reg.TipeJaminan.TipeJaminanId)
                ? reg.TipeJaminan.TipeJaminanId
                : "00000");

        var kelasId = !string.IsNullOrWhiteSpace(request.KelasId)
            ? request.KelasId
            : (!string.IsNullOrWhiteSpace(reg.Kelas.KelasId)
                ? reg.Kelas.KelasId
                : "3");

        var pasienName = reg.Pasien.PasienName;
        var pasienAlamat = "";
        var pasienAlamat2 = "";
        var pasienKota = "";
        var sex = "";

        if (_pasienRepo != null && !string.IsNullOrWhiteSpace(reg.Pasien.PasienId))
        {
            var pOpt = _pasienRepo.LoadEntity(PasienModel.Key(reg.Pasien.PasienId));
            if (pOpt.HasValue)
            {
                var p = pOpt.Value;
                var alamatArr = p.Person?.Alamat?.Alamat;
                pasienAlamat = (alamatArr != null && alamatArr.Length > 0) ? alamatArr[0] : "";
                pasienAlamat2 = (alamatArr != null && alamatArr.Length > 1) ? alamatArr[1] : "";
                pasienKota = p.Person?.Alamat?.Kota ?? "";
                sex = p.Person?.Gender ?? "";
            }
        }

        var trsDate = request.TransactionDate ?? DateTime.Now;
        var penjualanId = !string.IsNullOrWhiteSpace(request.PenjualanId)
            ? request.PenjualanId
            : PenjualanModel.GenerateUniqueId();

        var domainItems = new List<PenjualanItemModel>();
        int noUrut = 1;

        foreach (var item in request.Items)
        {
            var pricing = _pricingPort.CalculateItemPrice(new TrkTarifItemPricingRequest(
                BrgId: item.BrgId,
                TipeJaminanId: tipeJaminanId,
                TransactionDate: trsDate,
                Qty: item.Qty,
                SatuanId: item.SatuanId));

            var hargaSatuan = item.CustomHargaSatuan ?? pricing.HargaSatuan;
            var diskon = item.CustomDiskon ?? Math.Round(item.Qty * hargaSatuan * pricing.DiskonPersen / 100m, 2);
            var biaya = item.CustomBiaya ?? pricing.BiayaPerObat_r;
            var fee = item.CustomFee ?? (item.IsRacik ? pricing.BiayaPerResep_R : 0m);
            var taxProsen = item.CustomTaxProsen ?? 0m;

            var itemId = $"{penjualanId}{noUrut:D3}";
            var lineItem = PenjualanItemModel.Create(
                noUrut: noUrut++,
                penjualanItemId: itemId,
                brgId: item.BrgId,
                brgName: item.BrgName,
                satuanId: item.SatuanId,
                satuanName: item.SatuanName,
                qty: item.Qty,
                hargaSatuan: hargaSatuan,
                diskon: diskon,
                biaya: biaya,
                taxProsen: taxProsen,
                taxRupiah: 0m,
                fee: fee,
                etiket: item.Etiket,
                etiketQty: item.EtiketQty,
                etiketHari: item.EtiketHari,
                etiketKdPakai: item.EtiketKdPakai,
                etiketCatatan: item.EtiketCatatan,
                isRacik: item.IsRacik,
                isKomponen: item.IsKomponen,
                racikId: item.RacikId,
                isBeliBebas: false,
                nilaiKlaim: string.Equals(tipeJaminanId, "JKN00", StringComparison.OrdinalIgnoreCase) ? (item.Qty * hargaSatuan - diskon + biaya + fee) : 0m);

            domainItems.Add(lineItem);
        }

        var pembulatan = request.Pembulatan;
        if (pembulatan == 0)
        {
            var rounding = _pricingPort.CalculateRounding(domainItems.Sum(x => x.Total) - request.DiskonLain + request.BiayaLain);
            pembulatan = rounding.RoundingDifference;
        }

        var model = PenjualanModel.CreateDirect(
            regId: request.RegId,
            pasienName: pasienName,
            dokterId: request.DokterId,
            layananId: request.LayananId,
            tipeJaminanId: tipeJaminanId,
            kelasId: kelasId,
            petugasId: request.UserId,
            penjualanDate: trsDate,
            items: domainItems,
            layananResepId: request.LayananResepId,
            pasienAlamat: pasienAlamat,
            pasienAlamat2: pasienAlamat2,
            pasienKota: pasienKota,
            sex: sex,
            diskonLain: request.DiskonLain,
            biayaLain: request.BiayaLain,
            pembulatan: pembulatan,
            penjualanId: penjualanId);

        var isRanap = request.IsRanap ?? (reg.JenisReg == JenisRegEnum.RegInap);
        if (isRanap)
        {
            var chargeReq = new TrkBillingChargeRequest(
                PenjualanId: model.PenjualanId,
                RegId: request.RegId,
                LayananId: request.LayananId,
                KelasId: kelasId,
                PetugasId: request.UserId,
                TransactionDate: trsDate,
                SubTotal: model.SubTotal,
                Diskon: model.DiskonLain,
                Biaya: model.BiayaLain,
                Tax: model.SumTaxRupiah,
                Total: model.GrandTotal,
                Keterangan: $"Penjualan Farmasi DU Langsung ({model.PenjualanId})");

            var chargeId = _billingChargePort.PostBillingCharge(chargeReq);
            model.SetChargeId(chargeId);
        }

        _penjualanRepo.SaveChanges(model);
        return Task.FromResult(model.PenjualanId);
    }
}

/// <summary>
/// Handler pembentukan tagihan Jenis 3 (Penjualan Bebas Walk-in OTC Tanpa Registrasi).
/// </summary>
public class PenjualanCreateWalkInHandler : IRequestHandler<PenjualanCreateWalkInCmd, string>
{
    private readonly IPenjualanRepo _penjualanRepo;
    private readonly ITrkTarifPricingPort _pricingPort;
    private readonly IAptAuthorizationPolicy _auth;

    public PenjualanCreateWalkInHandler(
        IPenjualanRepo penjualanRepo,
        ITrkTarifPricingPort pricingPort,
        IAptAuthorizationPolicy auth)
    {
        _penjualanRepo = penjualanRepo;
        _pricingPort = pricingPort;
        _auth = auth;
    }

    public Task<string> Handle(PenjualanCreateWalkInCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(PenjualanCreateWalkInCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.PembeliName, nameof(request.PembeliName));
        Guard.Against.NullOrWhiteSpace(request.LayananId, nameof(request.LayananId));

        if (request.Items == null || request.Items.Count == 0)
            throw new ApotekDomainException("Penjualan walk-in bebas wajib memiliki minimal satu baris item.");

        var trsDate = request.TransactionDate ?? DateTime.Now;
        var penjualanId = !string.IsNullOrWhiteSpace(request.PenjualanId)
            ? request.PenjualanId
            : PenjualanModel.GenerateUniqueId();

        var domainItems = new List<PenjualanItemModel>();
        int noUrut = 1;

        foreach (var item in request.Items)
        {
            var pricing = _pricingPort.CalculateItemPrice(new TrkTarifItemPricingRequest(
                BrgId: item.BrgId,
                TipeJaminanId: "00000",
                TransactionDate: trsDate,
                Qty: item.Qty,
                SatuanId: item.SatuanId));

            var hargaSatuan = item.CustomHargaSatuan ?? pricing.HargaSatuan;
            var diskon = item.CustomDiskon ?? Math.Round(item.Qty * hargaSatuan * pricing.DiskonPersen / 100m, 2);
            var biaya = item.CustomBiaya ?? pricing.BiayaPerObat_r;
            var fee = item.CustomFee ?? (item.IsRacik ? pricing.BiayaPerResep_R : 0m);
            var taxProsen = item.CustomTaxProsen ?? 0m;

            var itemId = $"{penjualanId}{noUrut:D3}";
            var lineItem = PenjualanItemModel.Create(
                noUrut: noUrut++,
                penjualanItemId: itemId,
                brgId: item.BrgId,
                brgName: item.BrgName,
                satuanId: item.SatuanId,
                satuanName: item.SatuanName,
                qty: item.Qty,
                hargaSatuan: hargaSatuan,
                diskon: diskon,
                biaya: biaya,
                taxProsen: taxProsen,
                taxRupiah: 0m,
                fee: fee,
                etiket: item.Etiket,
                etiketQty: item.EtiketQty,
                etiketHari: item.EtiketHari,
                etiketKdPakai: item.EtiketKdPakai,
                etiketCatatan: item.EtiketCatatan,
                isRacik: item.IsRacik,
                isKomponen: item.IsKomponen,
                racikId: item.RacikId,
                isBeliBebas: true,
                nilaiKlaim: 0m);

            domainItems.Add(lineItem);
        }

        var pembulatan = request.Pembulatan;
        if (pembulatan == 0)
        {
            var rounding = _pricingPort.CalculateRounding(domainItems.Sum(x => x.Total) - request.DiskonLain + request.BiayaLain);
            pembulatan = rounding.RoundingDifference;
        }

        var model = PenjualanModel.CreateWalkIn(
            pembeliName: request.PembeliName,
            pembeliAlamat: request.PembeliAlamat,
            layananId: request.LayananId,
            petugasId: request.UserId,
            penjualanDate: trsDate,
            items: domainItems,
            pembeliKota: request.PembeliKota,
            sex: request.Sex,
            diskonLain: request.DiskonLain,
            biayaLain: request.BiayaLain,
            pembulatan: pembulatan,
            penjualanId: penjualanId);

        _penjualanRepo.SaveChanges(model);
        return Task.FromResult(model.PenjualanId);
    }
}

/// <summary>
/// Handler pembatalan penjualan (Soft-Void) dengan penegakan izin ADR-APT-003.
/// </summary>
public class PenjualanVoidHandler : IRequestHandler<PenjualanVoidCmd, Unit>
{
    private readonly IPenjualanRepo _penjualanRepo;
    private readonly ITataRekeningInvoicePermissionPort _permissionPort;
    private readonly ITrkBillingChargePort _billingChargePort;
    private readonly IAptAuthorizationPolicy _auth;

    public PenjualanVoidHandler(
        IPenjualanRepo penjualanRepo,
        ITataRekeningInvoicePermissionPort permissionPort,
        ITrkBillingChargePort billingChargePort,
        IAptAuthorizationPolicy auth)
    {
        _penjualanRepo = penjualanRepo;
        _permissionPort = permissionPort;
        _billingChargePort = billingChargePort;
        _auth = auth;
    }

    public Task<Unit> Handle(PenjualanVoidCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(PenjualanVoidCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.PenjualanId, nameof(request.PenjualanId));
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason));

        var modelOpt = _penjualanRepo.LoadEntity(new PenjualanKey(request.PenjualanId));
        if (!modelOpt.HasValue)
            throw new KeyNotFoundException($"Transaksi penjualan '{request.PenjualanId}' tidak ditemukan.");

        var model = modelOpt.Value;
        var allowsVoid = _permissionPort.AllowsVoid(model);
        var voidAt = request.VoidAt ?? DateTime.Now;

        // Domain method memeriksa allowsVoid dan menerapkan soft-void pada header & detail
        model.Void(request.UserId, voidAt, request.Reason, allowsVoid);

        // Jika terdapat link pembebanan billing rawat inap, picu pembatalan di Tata Rekening
        if (!string.IsNullOrWhiteSpace(model.TrsChargeId))
        {
            _billingChargePort.CancelBillingCharge(model.TrsChargeId, request.UserId, request.Reason);
        }

        _penjualanRepo.SaveChanges(model);
        return Task.FromResult(Unit.Value);
    }
}

/// <summary>
/// Handler pembaruan informasi serah terima obat fisik (Hook downstream Dispensing).
/// </summary>
public class PenjualanUpdateHandoverInfoHandler : IRequestHandler<PenjualanUpdateHandoverInfoCmd, Unit>
{
    private readonly IPenjualanRepo _penjualanRepo;
    private readonly IAptAuthorizationPolicy _auth;

    public PenjualanUpdateHandoverInfoHandler(
        IPenjualanRepo penjualanRepo,
        IAptAuthorizationPolicy auth)
    {
        _penjualanRepo = penjualanRepo;
        _auth = auth;
    }

    public Task<Unit> Handle(PenjualanUpdateHandoverInfoCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(PenjualanUpdateHandoverInfoCmd), request.UserId);
        Guard.Against.NullOrWhiteSpace(request.PenjualanId, nameof(request.PenjualanId));
        Guard.Against.NullOrWhiteSpace(request.PetugasSerah, nameof(request.PetugasSerah));
        Guard.Against.NullOrWhiteSpace(request.TrsMutasiId, nameof(request.TrsMutasiId));

        var modelOpt = _penjualanRepo.LoadEntity(new PenjualanKey(request.PenjualanId));
        if (!modelOpt.HasValue)
            throw new KeyNotFoundException($"Transaksi penjualan '{request.PenjualanId}' tidak ditemukan.");

        var model = modelOpt.Value;
        model.UpdateHandoverInfo(request.TglSerah, request.JamSerah, request.PetugasSerah, request.TrsMutasiId);

        _penjualanRepo.SaveChanges(model);
        return Task.FromResult(Unit.Value);
    }
}

#endregion
