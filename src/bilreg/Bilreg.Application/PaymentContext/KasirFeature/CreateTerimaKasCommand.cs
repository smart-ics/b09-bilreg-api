using System.Transactions;
using Ardalis.GuardClauses;
using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Domain.Shared;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public record CreateTerimaKasCommand(
    string ShiftId,
    string KasirId,
    string RegId,
    string PasienId,
    string PasienName,
    string OrderRef,
    string Keterangan,
    decimal TotalNilai,
    IEnumerable<PaymentItemInput> Payments,
    IEnumerable<KasirItemBayarInput>? Items = null) : IRequest<CreateTerimaKasResponse>;

public record CreateTerimaKasResponse(
    string TrsKasirId,
    string TglTrs,
    string JamTrs,
    string TglJamTrs,
    string JenisTrs,
    string ShiftId,
    string KasirId,
    string RegId,
    string PasienId,
    string PasienName,
    string OrderRef,
    string Keterangan,
    decimal TotalNilai,
    string StatusTrs,
    IEnumerable<PaymentItemResponse> Payments,
    IEnumerable<KasirItemBayarResponse>? Items = null);

public class CreateTerimaKasHandler : IRequestHandler<CreateTerimaKasCommand, CreateTerimaKasResponse>
{
    private readonly IKasirShiftRepo _kasirShiftRepo;
    private readonly ITrsKasirRepo _trsKasirRepo;
    private readonly IKasirStatusSyncDal _statusSyncDal;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly ILoketKasirDal _loketKasirDal;
    private readonly ITrsKasirItemDal _trsKasirItemDal;
    private readonly ITrsKasirLegacyDal _trsKasirLegacyDal;

    public CreateTerimaKasHandler(
        IKasirShiftRepo kasirShiftRepo,
        ITrsKasirRepo trsKasirRepo,
        IKasirStatusSyncDal statusSyncDal,
        ITglJamProvider tglJamProvider,
        ILoketKasirDal loketKasirDal,
        ITrsKasirItemDal trsKasirItemDal,
        ITrsKasirLegacyDal trsKasirLegacyDal)
    {
        _kasirShiftRepo = kasirShiftRepo;
        _trsKasirRepo = trsKasirRepo;
        _statusSyncDal = statusSyncDal;
        _tglJamProvider = tglJamProvider;
        _loketKasirDal = loketKasirDal;
        _trsKasirItemDal = trsKasirItemDal;
        _trsKasirLegacyDal = trsKasirLegacyDal;
    }

    public Task<CreateTerimaKasResponse> Handle(CreateTerimaKasCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.ShiftId, nameof(request.ShiftId));
        Guard.Against.NullOrWhiteSpace(request.KasirId, nameof(request.KasirId));
        Guard.Against.NegativeOrZero(request.TotalNilai, nameof(request.TotalNilai));
        Guard.Against.Null(request.Payments, nameof(request.Payments));

        // Rule 1: Validasi shift aktif dan milik kasir
        var shift = _kasirShiftRepo.GetKey(request.ShiftId)
            ?? throw new KeyNotFoundException($"Shift kasir {request.ShiftId} tidak ditemukan.");

        if (!shift.IsOpen)
        {
            throw new BusinessValidationException($"Shift kasir {request.ShiftId} sudah berstatus TUTUP dan tidak dapat menerima transaksi.");
        }

        if (!string.Equals(shift.KasirId, request.KasirId, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessValidationException($"Shift kasir {request.ShiftId} bukan milik kasir {request.KasirId}.");
        }

        // Rule 2: Resolusi CoaId tunai loket shift aktif jika MetodeBayar == TUNAI dan CoaId belum terisi
        var loket = !string.IsNullOrWhiteSpace(shift.Loket) ? _loketKasirDal.GetData(shift.Loket) : null;
        var defaultCashCoaId = !string.IsNullOrWhiteSpace(loket?.CoaId) ? loket.CoaId : "1110201";

        // Rule 3: Buat model transaksi kasir TrsKasirModel.CreateTerimaKas
        var domainPayments = request.Payments.Select(p =>
        {
            var isTunai = string.Equals(p.MetodeBayar?.Trim(), KasirConst.MetodeBayarTunai, StringComparison.OrdinalIgnoreCase);
            var coaId = isTunai && string.IsNullOrWhiteSpace(p.CoaId)
                ? defaultCashCoaId
                : (p.CoaId ?? string.Empty);
            var namaMetode = string.IsNullOrWhiteSpace(p.NamaMetodeBayar)
                ? (isTunai ? (loket?.NamaCoa ?? "KAS TUNAI") : (p.NamaBank ?? p.MetodeBayar ?? string.Empty))
                : p.NamaMetodeBayar;

            return TrsKasirPaymentType.Create(
                metodeBayar: p.MetodeBayar,
                nilaiBayar: p.NilaiBayar,
                nilaiKembalian: p.NilaiKembalian,
                nomorKartu: p.NomorKartu,
                nomorReferensi: p.NomorReferensi,
                namaBank: p.NamaBank,
                kodeJenisPayment: p.KodeJenisPayment,
                namaMetodeBayar: namaMetode,
                coaId: coaId);
        });

        var model = TrsKasirModel.CreateTerimaKas(
            shiftId: request.ShiftId,
            kasirId: request.KasirId,
            regId: request.RegId ?? string.Empty,
            pasienId: request.PasienId ?? string.Empty,
            pasienName: request.PasienName ?? string.Empty,
            orderRef: request.OrderRef ?? string.Empty,
            keterangan: request.Keterangan ?? string.Empty,
            totalNilai: request.TotalNilai,
            payments: domainPayments,
            trsTime: _tglJamProvider.Now);

        // Siapkan DTO item dan legacy
        var itemsInput = request.Items?.ToList() ?? new List<KasirItemBayarInput>();
        var itemDtos = itemsInput.Select((item, idx) => new TrsKasirItemDto(
            KasirItemId: $"{model.TrsKasirId}-{idx + 1:D2}",
            TrsKasirId: model.TrsKasirId,
            NoUrut: idx + 1,
            BillId: item.BillId,
            RegId: string.IsNullOrWhiteSpace(item.RegId) ? model.RegId : item.RegId,
            LayananId: item.LayananId ?? string.Empty,
            Keterangan: item.Keterangan ?? string.Empty,
            NilaiBill: item.NilaiBill > 0 ? item.NilaiBill : (item.NilaiBayar > 0 ? item.NilaiBayar : model.TotalNilai),
            NilaiBayar: item.NilaiBayar > 0 ? item.NilaiBayar : (item.NilaiBill > 0 ? item.NilaiBill : model.TotalNilai),
            crttgl: model.TglTrs,
            crtjam: model.JamTrs,
            crtusr: model.KasirId
        )).ToList();

        // Siapkan legacy header & details
        var sumKas = model.Payments.Where(p => p.MetodeBayar == KasirConst.MetodeBayarTunai).Sum(p => p.NilaiNetto);
        var sumBank = model.Payments.Where(p => p.MetodeBayar == KasirConst.MetodeBayarDebit || p.MetodeBayar == KasirConst.MetodeBayarTransfer || p.MetodeBayar == KasirConst.MetodeBayarQris).Sum(p => p.NilaiNetto);
        var sumKartuKredit = model.Payments.Where(p => p.MetodeBayar == KasirConst.MetodeBayarKredit).Sum(p => p.NilaiNetto);

        var cashPayment = model.Payments.FirstOrDefault(p => p.MetodeBayar == KasirConst.MetodeBayarTunai);
        var bankPayment = model.Payments.FirstOrDefault(p => p.MetodeBayar == KasirConst.MetodeBayarDebit || p.MetodeBayar == KasirConst.MetodeBayarTransfer || p.MetodeBayar == KasirConst.MetodeBayarQris);
        var creditPayment = model.Payments.FirstOrDefault(p => p.MetodeBayar == KasirConst.MetodeBayarKredit);

        var legacyHdr = new TrsKasirLegacyHdrDto(
            trsKasirId: model.TrsKasirId,
            tglTrs: model.TglTrs,
            jamTrs: model.JamTrs,
            petugasId: model.KasirId,
            regId: model.RegId,
            sumKas: sumKas,
            sumBank: sumBank,
            sumKartuKredit: sumKartuKredit,
            sumAll: model.TotalNilai,
            kdRekKas: cashPayment?.CoaId ?? defaultCashCoaId,
            kdRekBank: bankPayment?.CoaId ?? string.Empty,
            kdRekKartuKredit: creditPayment?.CoaId ?? string.Empty,
            noKartuKredit: creditPayment?.NomorKartu ?? string.Empty,
            kdJenisBank: bankPayment?.KodeJenisPayment ?? string.Empty,
            kdJenisKartuKredit: creditPayment?.KodeJenisPayment ?? string.Empty,
            noBank: bankPayment?.NomorReferensi ?? (bankPayment?.NomorKartu ?? string.Empty),
            crtUsr: model.KasirId
        );

        var legacyDtls = new List<TrsKasirLegacyDtlDto>();
        if (itemsInput.Count > 0)
        {
            legacyDtls = itemsInput.Select((item, idx) =>
            {
                var itemNilaiBayar = item.NilaiBayar > 0 ? item.NilaiBayar : (item.NilaiBill > 0 ? item.NilaiBill : model.TotalNilai);
                var ratio = model.TotalNilai > 0 ? (itemNilaiBayar / model.TotalNilai) : 0;
                var itemKas = itemsInput.Count == 1 ? sumKas : Math.Round(sumKas * ratio, 2);
                var itemBank = itemsInput.Count == 1 ? sumBank : Math.Round(sumBank * ratio, 2);
                var itemKredit = itemsInput.Count == 1 ? sumKartuKredit : Math.Round(sumKartuKredit * ratio, 2);

                var isObat = (item.LayananId ?? string.Empty).StartsWith("4") || (item.Keterangan ?? string.Empty).Contains("Obat", StringComparison.OrdinalIgnoreCase);
                var isTitipan = (item.LayananId ?? string.Empty).StartsWith("TIT") || (item.Keterangan ?? string.Empty).Contains("Titipan", StringComparison.OrdinalIgnoreCase);
                var isJasa = !isObat && !isTitipan;

                return new TrsKasirLegacyDtlDto(
                    trsKasirId: model.TrsKasirId,
                    noUrut: idx + 1,
                    trsBillId: item.BillId,
                    layananId: item.LayananId ?? string.Empty,
                    keterangan: item.Keterangan ?? string.Empty,
                    nilaiBill: item.NilaiBill > 0 ? item.NilaiBill : itemNilaiBayar,
                    nilaiKas: itemKas,
                    nilaiBank: itemBank,
                    nilaiKartuKredit: itemKredit,
                    isVoid: false,
                    isJasa: isJasa,
                    isObat: isObat,
                    isTitipan: isTitipan
                );
            }).ToList();
        }
        else if (!string.IsNullOrWhiteSpace(model.OrderRef))
        {
            legacyDtls.Add(new TrsKasirLegacyDtlDto(
                trsKasirId: model.TrsKasirId,
                noUrut: 1,
                trsBillId: model.OrderRef,
                layananId: string.Empty,
                keterangan: model.Keterangan,
                nilaiBill: model.TotalNilai,
                nilaiKas: sumKas,
                nilaiBank: sumBank,
                nilaiKartuKredit: sumKartuKredit,
                isVoid: false,
                isJasa: true,
                isObat: false,
                isTitipan: false
            ));
        }

        // Rule 4: Eksekusi transaksi atomik
        using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            // 4a. Simpan model kasir baru (BILRG_TrsKasir & BILRG_TrsKasirPayment)
            _trsKasirRepo.Save(model);

            // 4b. Simpan rincian item ke BILRG_TrsKasirItem jika ada
            if (itemDtos.Count > 0)
            {
                _trsKasirItemDal.Insert(itemDtos);
            }

            // 4c. Dual-write legacy (ta_trs_deposit_khusus & khusus2)
            _trsKasirLegacyDal.Insert(legacyHdr, legacyDtls);

            // 4d. Sinkronisasi status hilir (ta_trs_deposit & ta_registrasi3)
            if (!string.IsNullOrWhiteSpace(model.OrderRef) || !string.IsNullOrWhiteSpace(model.RegId))
            {
                _statusSyncDal.SyncTerima(
                    orderRef: model.OrderRef,
                    kasirId: model.KasirId,
                    tglTrs: model.TglTrs,
                    jamTrs: model.JamTrs,
                    trsKasirId: model.TrsKasirId,
                    totalNilai: model.TotalNilai,
                    regId: model.RegId);
            }

            scope.Complete();
        }

        var paymentResponses = model.Payments.Select(p => new PaymentItemResponse(
            PaymentRowId: p.PaymentRowId,
            NoUrut: p.NoUrut,
            MetodeBayar: p.MetodeBayar,
            NilaiBayar: p.NilaiBayar,
            NilaiKembalian: p.NilaiKembalian,
            NilaiNetto: p.NilaiNetto,
            NomorKartu: p.NomorKartu,
            NomorReferensi: p.NomorReferensi,
            NamaBank: p.NamaBank,
            KodeJenisPayment: p.KodeJenisPayment,
            NamaMetodeBayar: p.NamaMetodeBayar,
            CoaId: p.CoaId));

        var itemResponses = itemDtos.Select(i => new KasirItemBayarResponse(
            KasirItemId: i.KasirItemId,
            NoUrut: i.NoUrut,
            BillId: i.BillId,
            RegId: i.RegId,
            LayananId: i.LayananId,
            Keterangan: i.Keterangan,
            NilaiBill: i.NilaiBill,
            NilaiBayar: i.NilaiBayar));

        var response = new CreateTerimaKasResponse(
            TrsKasirId: model.TrsKasirId,
            TglTrs: model.TglTrs,
            JamTrs: model.JamTrs,
            TglJamTrs: model.TglJamTrsStr,
            JenisTrs: model.JenisTrs,
            ShiftId: model.ShiftId,
            KasirId: model.KasirId,
            RegId: model.RegId,
            PasienId: model.PasienId,
            PasienName: model.PasienName,
            OrderRef: model.OrderRef,
            Keterangan: model.Keterangan,
            TotalNilai: model.TotalNilai,
            StatusTrs: model.StatusTrs,
            Payments: paymentResponses,
            Items: itemResponses);

        return Task.FromResult(response);
    }
}
