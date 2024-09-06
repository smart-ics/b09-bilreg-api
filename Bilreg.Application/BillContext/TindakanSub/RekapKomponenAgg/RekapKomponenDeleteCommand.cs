using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using MediatR;

namespace Bilreg.Application.BillContext.TindakanSub.RekapKomponenAgg
{
    public record RekapKomponenDeleteCommand(string RekapKomponenId) : IRequest, IRekapKomponenKey;

    public class RekapKomponenDeleteHandler : IRequestHandler<RekapKomponenDeleteCommand>
    {
        private readonly IRekapKomponenWriter _writer;

        public RekapKomponenDeleteHandler(RekapKomponenWriter writer)
        {
            _writer = writer;
        }

        public Task Handle(RekapKomponenDeleteCommand request, CancellationToken cancellationToken)
        {
            // GUARD
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.RekapKomponenId);

            // WRITER
            _writer.Delete(request);
            return Task.CompletedTask;
        }
    }
}
