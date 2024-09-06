using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using MediatR;

namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg
{
    public record GrupKomponenDeleteCommand(string GrupKomponenId) : IRequest, IGrupKomponenKey;

    public class GrupKomponenDeleteHandler : IRequestHandler<GrupKomponenDeleteCommand>
    {
        private readonly IGrupKomponenWriter _writer;

        public GrupKomponenDeleteHandler(IGrupKomponenWriter writer)
        {
            _writer = writer;
        }

        public Task Handle(GrupKomponenDeleteCommand request, CancellationToken cancellationToken)
        {
            // GUARD
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.GrupKomponenId);

            // WRITER
            _writer.Delete(request);
            return Task.CompletedTask;
        }
    }
}
