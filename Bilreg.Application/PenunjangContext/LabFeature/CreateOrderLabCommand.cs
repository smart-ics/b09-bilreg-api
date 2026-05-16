using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PenunjangContext.LabFeature;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.PenunjangContext.LabFeature
{
    public record CreateOrderLabCommand(string TindakanId)
        : IRequest<CreateOrderLabResponse>;
    public record CreateOrderLabResponse(string OrderLabId);
    public class CreateOrderLabHandler : IRequestHandler<CreateOrderLabCommand, CreateOrderLabResponse>
    {
        private readonly IOrderLabRepo _orderLabRepo;
        private readonly IRegAktifRepo _regRepo;
        private readonly ITindakanRepo _tindakanRepo;
        public CreateOrderLabHandler(IOrderLabRepo orderLabRepo, IRegAktifRepo regRepo)
        {
            _orderLabRepo = orderLabRepo;
            _regRepo = regRepo;
        }
        public Task<CreateOrderLabResponse> Handle(CreateOrderLabCommand request, CancellationToken cancellationToken)
        {
            // GUARD
            Guard.Against.NullOrEmpty(request.TindakanId, nameof(request.TindakanId));
            // BUILD
             var orderLab = Build(request.TindakanId);

            throw new NotImplementedException();
        }

        private OrderLabModel Build(string tindakanId)
        {
            var tindakan = _tindakanRepo.LoadEntity(TindakanModel.Key(tindakanId))
                .GetValueOrThrow("Tindakan tidak ditemukan");
            var reg = _regRepo.LoadEntity(RegModel.Key(tindakan.Reg.RegId))
                .GetValueOrThrow("Reg tidak ditemukan");
           // var tarif = tindakan.Tarif;
            var orderLab = new OrderLabModel
            (
                Guid.NewGuid().ToString(),
                tindakan.TindakanId,
                DateTime.Now,
                OrderStatusEnum.TestOrdered,
                reg.Pasien, "", PegType.Default,
                tindakan.Layanan,
                new List<TarifReff>().Add(tindakan.Tarif))
            );
        }
    }
}
