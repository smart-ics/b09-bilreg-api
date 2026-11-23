using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;

public record GetTransportQuery(string TrsTransportId) : IRequest<TransportGetResponse>, ITrsTransportKey;

public record TransportGetResponse(
    string TrsTransportId,
    string RegId,
    string LayananId,
    DateTime TglTrs,
    string PetugasId,
    string Status,
    string TipePerjalanan,
    string MetodeTarif,
    string AmbulanceId,
    string DriverId,
    string NoPolisi,
    string PerawatId,
    string DokterId,
    string LokasiAsal,
    string LokasiTujuan,
    string TujuanTransportId,
    decimal Konstanta,
    DateTime TglRequest,
    DateTime? TglDispatch,
    DateTime? TglArrival,
    DateTime? TglReturn,
    decimal StartOdometer,
    decimal EndOdometer,
    decimal TotalJarakKm,
    decimal SubTotal,
    decimal Diskon,
    decimal Total,
    string JaminanId,
    bool IsNonReg,
    bool IsPostedBilling,
    string AlasanBatal,
    decimal BiayaBatal,
    string Keterangan,
    IEnumerable<TransportItemResponse> ListItems,
    bool IsVoid = false);

public record TransportItemResponse(
    string DetilTarifId,
    decimal Tarif,
    decimal Qty,
    string PetugasMedisId,
    decimal Diskon,
    decimal SubTotal,
    bool IsVoid,
    string Keterangan);

public static class TransportResponseMapper
{
    public static TransportGetResponse ToResponse(TrsTransportModel t)
    {
        var items = t.ListItems.Select(i => new TransportItemResponse(
            i.DetilTarifId,
            i.Tarif,
            i.Qty,
            i.PetugasMedisId,
            i.Diskon,
            i.SubTotal,
            i.IsVoid,
            i.Keterangan)).ToList();

        return new TransportGetResponse(
            t.TrsTransportId,
            t.RegId,
            t.LayananId,
            t.TglTrs,
            t.PetugasId,
            t.Status.ToString(),
            t.TipePerjalanan.ToString(),
            t.MetodeTarif.ToString(),
            t.AmbulanceId,
            t.DriverId,
            t.NoPolisi,
            t.PerawatId,
            t.DokterId,
            t.LokasiAsal,
            t.LokasiTujuan,
            t.TujuanTransportId,
            t.Konstanta,
            t.TglRequest,
            t.TglDispatch,
            t.TglArrival,
            t.TglReturn,
            t.StartOdometer,
            t.EndOdometer,
            t.TotalJarakKm,
            t.SubTotal,
            t.Diskon,
            t.Total,
            t.JaminanId,
            t.IsNonReg,
            t.IsPostedBilling,
            t.AlasanBatal,
            t.BiayaBatal,
            t.Keterangan,
            items,
            t.IsVoid);
    }
}

public class GetTransportHandler : IRequestHandler<GetTransportQuery, TransportGetResponse>
{
    private readonly ITrsTransportRepo _trsTransportRepo;

    public GetTransportHandler(ITrsTransportRepo trsTransportRepo)
    {
        _trsTransportRepo = trsTransportRepo;
    }

    public Task<TransportGetResponse> Handle(GetTransportQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TrsTransportId, nameof(request.TrsTransportId));

        var transport = _trsTransportRepo.LoadEntity(request).Match(
            onSome: t => t,
            onNone: () => throw new KeyNotFoundException($"Transaksi transport dengan id '{request.TrsTransportId}' tidak ditemukan."));

        return Task.FromResult(TransportResponseMapper.ToResponse(transport));
    }
}
