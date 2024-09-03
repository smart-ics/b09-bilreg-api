using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using MediatR;

namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg
{
    public record GrupKomponenSaveCommand(string GrupKomponenId, string GrupKomponenName) :IRequest,IGrupKomponenKey;

    public class GrupKomponenSaveHandler : IRequestHandler<GrupKomponenSaveCommand>
    {
        private readonly IGrupKomponenDal _grupKomponenDal;
        private readonly GrupKomponenWriter _writer;

        public Task Handle(GrupKomponenSaveCommand request, CancellationToken cancellationToken)
        {
            // GUARD
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.GrupKomponenId);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.GrupKomponenName);
            var grupKomponenData = _grupKomponenDal.GetData(request)
                ?? throw new KeyNotFoundException($"Grup Komponen with id{request.GrupKomponenId} not found.");

            // BUILD
            var grupKomponen = _grupKomponenDal.GetData(request)
                ?? new GrupKomponenModel(request.GrupKomponenId, request.GrupKomponenName);
            
            grupKomponen.Set(grupKomponenData);

            // WRITE
            _ = _writer.Save(grupKomponen);

            return Task.CompletedTask;


        }
    }
}
