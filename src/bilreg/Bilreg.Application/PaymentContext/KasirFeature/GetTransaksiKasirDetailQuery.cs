using Ardalis.GuardClauses;
using Bilreg.Domain.PaymentContext.KasirFeature;
using MediatR;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public record GetTransaksiKasirDetailQuery(string TrsKasirId) : IRequest<TransaksiKasirDetailResponse?>, ITrsKasirKey;

public record TransaksiKasirDetailResponse(
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
    bool IsVoid,
    string TglVoid,
    string JamVoid,
    string PetugasVoid,
    string AlasanVoid,
    IEnumerable<PaymentItemResponse> Payments);

public class GetTransaksiKasirDetailHandler : IRequestHandler<GetTransaksiKasirDetailQuery, TransaksiKasirDetailResponse?>
{
    private readonly ITrsKasirRepo _trsKasirRepo;

    public GetTransaksiKasirDetailHandler(ITrsKasirRepo trsKasirRepo)
    {
        _trsKasirRepo = trsKasirRepo;
    }

    public Task<TransaksiKasirDetailResponse?> Handle(GetTransaksiKasirDetailQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.TrsKasirId, nameof(request.TrsKasirId));

        var model = _trsKasirRepo.GetKey(request.TrsKasirId);
        if (model is null)
            return Task.FromResult<TransaksiKasirDetailResponse?>(null);

        var paymentResponses = model.Payments.Select(p => new PaymentItemResponse(
            PaymentRowId: p.PaymentRowId,
            NoUrut: p.NoUrut,
            MetodeBayar: p.MetodeBayar,
            NilaiBayar: p.NilaiBayar,
            NilaiKembalian: p.NilaiKembalian,
            NilaiNetto: p.NilaiNetto,
            NomorKartu: p.NomorKartu,
            NomorReferensi: p.NomorReferensi,
            NamaBank: p.NamaBank));

        var response = new TransaksiKasirDetailResponse(
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
            IsVoid: model.IsVoid,
            TglVoid: model.TglVoid,
            JamVoid: model.JamVoid,
            PetugasVoid: model.PetugasVoid,
            AlasanVoid: model.AlasanVoid,
            Payments: paymentResponses);

        return Task.FromResult<TransaksiKasirDetailResponse?>(response);
    }
}
