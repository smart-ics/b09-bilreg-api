using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.AdmisiContext.LayananSub.LayananAgg
{
    public record LayananSetActiveCommand(string LayananId) : IRequest, ILayananKey;

    public class LayananSetActiveCommandHandler : IRequestHandler<LayananSetActiveCommand>
    {
        private readonly ILayananDal _layananDal;
        private readonly ILayananWriter _writer;

        public LayananSetActiveCommandHandler(ILayananDal layananDal, ILayananWriter writer )
        {
            _layananDal = layananDal;
            _writer = writer;
        }

        public Task Handle(LayananSetActiveCommand request, CancellationToken cancellationToken)
        {
            Guard.IsNotNull(request);
            Guard.IsNotWhiteSpace(request.LayananId);

            var status = _layananDal.GetData(request)
                ?? throw new KeyNotFoundException($"Layanan id {request.LayananId} not found");

            status.SetAktif();
            _writer.Save(status);
            return Task.CompletedTask;
        }
    }
}
