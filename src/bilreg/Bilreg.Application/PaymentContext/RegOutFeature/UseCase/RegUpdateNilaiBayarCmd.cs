using Bilreg.Application.PaymentContext.RegOutFeature.RegOutAgg;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.RegOutFeature;
using MediatR;

namespace Bilreg.Application.PaymentContext.RegOutFeature.UseCase;

public record RegUpdateNilaiBayarCmd(
    string RegId,
    string CaraBayarId,
    int JasaObat,  // 0 = Jasa, 1 = Obat
    decimal Nilai) : IRequest, IRegKey;

public class RegUpdateNilaiBayarHandler : IRequestHandler<RegUpdateNilaiBayarCmd>
{
    private readonly IRegPembayaranRepo _regPembayaranRepo;

    public RegUpdateNilaiBayarHandler(IRegPembayaranRepo regPembayaranRepo)
    {
        _regPembayaranRepo = regPembayaranRepo;
    }

    public Task Handle(RegUpdateNilaiBayarCmd request, CancellationToken cancellationToken)
    {
        var existingList = _regPembayaranRepo.ListData(request);
        var existingData = existingList.FirstOrDefault(x => x.CaraBayarId == request.CaraBayarId);

        if (existingData == null)
            throw new KeyNotFoundException($"Cara Bayar {request.CaraBayarId} tidak aktif");

        var newJasa = request.JasaObat == 0 ? request.Nilai : existingData.NilaiJasa;
        var newObat = request.JasaObat == 1 ? request.Nilai : existingData.NilaiObat;
        var newSubTotal = newJasa + newObat;

        var updatedModel = new RegPembayaranType(
            request.RegId,
            request.CaraBayarId,
            existingData.CaraBayarName,
            newJasa,
            newObat,
            newSubTotal
        );
        _regPembayaranRepo.Update(updatedModel);

        return Task.CompletedTask;
    }
}