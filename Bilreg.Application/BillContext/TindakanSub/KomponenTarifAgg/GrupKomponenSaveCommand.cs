using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using MediatR;

namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg
{
    public record GrupKomponenSaveCommand(string GrupKomponenId, string GrupKomponenName) :IRequest,IGrupKomponenKey;

    public class GrupKomponenSaveHandler : IRequestHandler<GrupKomponenSaveCommand>
    {
        private readonly IGrupKomponenDal _grupKomponenDal;
        private readonly GrupKomponenWriter _writer;

        public GrupKomponenSaveHandler(IGrupKomponenDal grupKomponenDal, GrupKomponenWriter writer)
        {
            _grupKomponenDal = grupKomponenDal;
            _writer = writer;
        }

        public Task Handle(GrupKomponenSaveCommand request, CancellationToken cancellationToken)
        {
            // GUARD
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.GrupKomponenId);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.GrupKomponenName);

            //  BUILD
            var grupKomponen = _grupKomponenDal.GetData(request)
                ?? new GrupKomponenModel(request.GrupKomponenId, request.GrupKomponenName);
            grupKomponen.SetName(request.GrupKomponenName);
            grupKomponen.Set(grupKomponen);

            // WRITE
            _ = _writer.Save(grupKomponen);
            return Task.CompletedTask;


        }
    }
}
