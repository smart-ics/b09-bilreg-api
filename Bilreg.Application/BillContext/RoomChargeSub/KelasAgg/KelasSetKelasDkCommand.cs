using Bilreg.Application.BillContext.RoomChargeSub.KelasDkAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KelasAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KelasDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.BillContext.RoomChargeSub.KelasAgg
{
    public record KelasSetKelasDkCommand(string KelasId, string KelasDkId): IRequest,IKelasKey,IKelasDkKey;
    public class KelasSetKelasDkHandler : IRequestHandler<KelasSetKelasDkCommand>
    {
        private readonly IKelasDal _kelasDal;
        private readonly IKelasDkDal _kelasDkDal;

        public KelasSetKelasDkHandler(IKelasDal kelasDal, IKelasDkDal kelasDkDal)
        {
            _kelasDal = kelasDal;
            _kelasDkDal = kelasDkDal;
        }

        public Task Handle(KelasSetKelasDkCommand request, CancellationToken cancellationToken)
        {
            // Guard
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.KelasDkId);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.KelasId);

            // Build
            var kelas = _kelasDal.GetData(request) ?? throw new KeyNotFoundException("Kelas Not Found");
            var kelasDk = _kelasDkDal.GetData(request) ?? throw new KeyNotFoundException("KelasDk Not Found");
            kelas.Set(kelasDk);

            // Write
            return Task.CompletedTask;
        }
    }
}
