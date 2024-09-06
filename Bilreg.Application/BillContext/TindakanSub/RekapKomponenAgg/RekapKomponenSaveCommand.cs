using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using MediatR;

namespace Bilreg.Application.BillContext.TindakanSub.RekapKomponenAgg
{
    public record RekapKomponenSaveCommand(string RekapKomponenId, string RekapKomponenName, decimal RekapKomponenUrut):IRequest,IRekapKomponenKey;

    public class RekapKomponenSaveHandler : IRequestHandler<RekapKomponenSaveCommand>
    {
        private readonly IRekapKomponenDal _rekapKomponenDal;
        private readonly RekapKomponenWriter _writer;

        public RekapKomponenSaveHandler(IRekapKomponenDal rekapKomponenDal, RekapKomponenWriter writer)
        {
            _rekapKomponenDal = rekapKomponenDal;
            _writer = writer;
        }

        public Task Handle(RekapKomponenSaveCommand request, CancellationToken cancellationToken)
        {
            // GUARD
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.RekapKomponenId);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.RekapKomponenName);

            // BUILD
            var rekapKomponen = _rekapKomponenDal.GetData(request)
                ?? new RekapKomponenModel(request.RekapKomponenId, request.RekapKomponenName, request.RekapKomponenUrut);
            rekapKomponen.SetName(request.RekapKomponenName);
            rekapKomponen.SetUrut(request.RekapKomponenUrut);
            rekapKomponen.Set(rekapKomponen);

            // WRITE
            _ = _writer.Save(rekapKomponen);
            return Task.CompletedTask;
        }
    }
}
