using System.Transactions;
using Ardalis.GuardClauses;
using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Domain.Shared;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public record CreateKeluarKasCommand(
    string ShiftId,
    string KasirId,
    string RegId,
    string PasienId,
    string PasienName,
    string OrderRef,
    string Keterangan,
    decimal TotalNilai,
    IEnumerable<PaymentItemInput> Payments) : IRequest<CreateKeluarKasResponse>;

public record CreateKeluarKasResponse(
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
    IEnumerable<PaymentItemResponse> Payments);

public class CreateKeluarKasHandler : IRequestHandler<CreateKeluarKasCommand, CreateKeluarKasResponse>
{
    private readonly IKasirShiftRepo _kasirShiftRepo;
    private readonly ITrsKasirRepo _trsKasirRepo;
    private readonly IKasirStatusSyncDal _statusSyncDal;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly ILoketKasirDal _loketKasirDal;
    private readonly ITrsKasirLegacyDal _trsKasirLegacyDal;

    public CreateKeluarKasHandler(
        IKasirShiftRepo kasirShiftRepo,
        ITrsKasirRepo trsKasirRepo,
        IKasirStatusSyncDal statusSyncDal,
        ITglJamProvider tglJamProvider,
        ILoketKasirDal loketKasirDal,
        ITrsKasirLegacyDal trsKasirLegacyDal)
    {
        _kasirShiftRepo = kasirShiftRepo;
        _trsKasirRepo = trsKasirRepo;
        _statusSyncDal = statusSyncDal;
        _tglJamProvider = tglJamProvider;
        _loketKasirDal = loketKasirDal;
        _trsKasirLegacyDal = trsKasirLegacyDal;
    }

    public Task<CreateKeluarKasResponse> Handle(CreateKeluarKasCommand request, CancellationToken cancellationToken)
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
            throw new BusinessValidationException($"Shift kasir {request.ShiftId} sudah berstatus TUTUP dan tidak dapat mengeluarkan kas.");
        }

        if (!string.Equals(shift.KasirId, request.KasirId, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessValidationException($"Shift kasir {request.ShiftId} bukan milik kasir {request.KasirId}.");
        }

        // Rule 2: Resolusi CoaId tunai loket shift aktif jika MetodeBayar == TUNAI dan CoaId belum terisi
        var loket = !string.IsNullOrWhiteSpace(shift.Loket) ? _loketKasirDal.GetData(shift.Loket) : null;
        var defaultCashCoaId = !string.IsNullOrWhiteSpace(loket?.CoaId) ? loket.CoaId : "1110201";

        // Rule 3: Buat model transaksi kasir TrsKasirModel.CreateKeluarKas
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

        var model = TrsKasirModel.CreateKeluarKas(
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
        if (!string.IsNullOrWhiteSpace(model.OrderRef))
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

        // Rule 4: Transaksi atomik
        using (var scope = TransHelper.NewScope())
        {
            _trsKasirRepo.Save(model);
            _trsKasirLegacyDal.Insert(legacyHdr, legacyDtls);

            if (!string.IsNullOrWhiteSpace(model.OrderRef) || !string.IsNullOrWhiteSpace(model.RegId))
            {
                _statusSyncDal.SyncKeluar(
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

        var response = new CreateKeluarKasResponse(
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
            Payments: paymentResponses);

        return Task.FromResult(response);
    }
}
