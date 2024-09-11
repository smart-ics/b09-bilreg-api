using Bilreg.Application.AdmisiContext.LayananSub.LayananDkAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.LayananDkAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.AdmisiContext.LayananSub.LayananAgg
{
    public record LayananSetLayananDkCommand(string LayananId, string LayananDkId) : IRequest, ILayananKey, ILayananDkKey;

    public class LayananSetLayananDkHandler : IRequestHandler<LayananSetLayananDkCommand>
    {
        private readonly ILayananDal _layananDal;
        private readonly ILayananDkDal _layananDkDal;
        private readonly ILayananWriter _writer;

        public LayananSetLayananDkHandler(ILayananDal layananDal, ILayananDkDal layananDkDal, ILayananWriter writer)
        {
            _layananDal = layananDal;
            _layananDkDal = layananDkDal;
            _writer = writer;
        }


        public Task Handle(LayananSetLayananDkCommand request, CancellationToken cancellationToken)
        {
            Guard.IsNotNull(request);
            Guard.IsNotWhiteSpace(request.LayananId);
            Guard.IsNotWhiteSpace(request.LayananDkId);

            var layanan = _layananDal.GetData(request)
                ?? throw new KeyNotFoundException($"Layanan id {request.LayananId} not found");
            var layananDk = _layananDkDal.GetData(request)
                ?? throw new KeyNotFoundException($"LayananDk id {request.LayananDkId} not found"); ;
            layanan.SetLayananDk(layananDk);

            _writer.Save(layanan);
            return Task.CompletedTask;
        }
    }
}
