using Bilreg.Application.AdmisiContext.LayananSub.TipeLayananDkAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.TipeLayananDkAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.AdmisiContext.LayananSub.LayananAgg
{
    public record LayananSetTipeLayananDkCommand(string LayananId, string TipeLayananDkId) : IRequest, ILayananKey, ITipeLayananDkKey;

    public class LayananSetTipeLayananDkHandler : IRequestHandler<LayananSetTipeLayananDkCommand>
    {
        private readonly ILayananDal _layananDal;
        private readonly ITipeLayananDkDal _tipeLayananDkDal;
        private readonly ILayananWriter _writer;

        public LayananSetTipeLayananDkHandler(ILayananDal layananDal, ITipeLayananDkDal tipeLayananDkDal, ILayananWriter writer)
        {
            _layananDal = layananDal;
            _tipeLayananDkDal = tipeLayananDkDal;
            _writer = writer;
        }


        public Task Handle(LayananSetTipeLayananDkCommand request, CancellationToken cancellationToken)
        {
            Guard.IsNotNull(request);
            Guard.IsNotWhiteSpace(request.LayananId);
            Guard.IsNotWhiteSpace(request.TipeLayananDkId);

            var layanan = _layananDal.GetData(request)
                ?? throw new KeyNotFoundException($"Layanan id {request.LayananId} not found");
            var tipeLayananDk = _tipeLayananDkDal.GetData(request)
                ?? throw new KeyNotFoundException($"TipeLayananDk id {request.TipeLayananDkId} not found"); ;

            layanan.SetTipeLayananDk(tipeLayananDk);

            _writer.Save(layanan);
            return Task.CompletedTask;
        }
    }
}
