using Bilreg.Application.PaymentContext.RegOutFeature.RegOutAgg;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.RegOutFeature;
using MediatR;

namespace Bilreg.Application.PaymentContext.RegOutFeature.UseCase;

public record RegAddPembayaranCmd(
    string RegId,
    string CaraBayarId,
    string CaraBayarName,
    decimal Jasa,
    decimal Obat) : IRequest, IRegKey;

public class RegAddPembayaranHandler : IRequestHandler<RegAddPembayaranCmd>
{
    private readonly IRegPembayaranRepo _regPembayaranRepo;
    public RegAddPembayaranHandler(IRegPembayaranRepo regPembayaranRepo)
    {
        _regPembayaranRepo = regPembayaranRepo;
    }
    public Task Handle(RegAddPembayaranCmd request, CancellationToken cancellationToken)
    {
        var nilaiSubTotal = request.Jasa + request.Obat;
        var model = new RegPembayaranType(
            request.RegId,
            request.CaraBayarId,
            request.CaraBayarName,
            request.Jasa,
            request.Obat,
            nilaiSubTotal
        );
        _regPembayaranRepo.Insert(model);

        return Task.CompletedTask;
    }
}