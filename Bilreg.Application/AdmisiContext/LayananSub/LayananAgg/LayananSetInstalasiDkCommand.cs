using Bilreg.Application.AdmisiContext.LayananSub.InstalasiDkAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiDkAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.AdmisiContext.LayananSub.LayananAgg
{
    public record LayananSetInstalasiDkCommand(string LayananId, string InstalasiDkId):IRequest,ILayananKey,IInstalasiDkKey;

    public class LayananSetInstalasiDkHandler : IRequestHandler<LayananSetInstalasiDkCommand>
    {
        private readonly ILayananDal _layananDal;
        private readonly IInstalasiDkDal _instalasiDkDal;
        private readonly ILayananWriter _writer;

        public LayananSetInstalasiDkHandler(ILayananDal layananDal, IInstalasiDkDal instalasiDkDal, ILayananWriter writer)
        {
            _layananDal = layananDal;
            _instalasiDkDal = instalasiDkDal;
            _writer = writer;
        }


        public Task Handle(LayananSetInstalasiDkCommand request, CancellationToken cancellationToken)
        {
            Guard.IsNotNull(request);
            Guard.IsNotWhiteSpace(request.LayananId);
            Guard.IsNotWhiteSpace(request.InstalasiDkId);

            var layanan = _layananDal.GetData(request)
                ?? throw new KeyNotFoundException($"Layanan id {request.LayananId} not found");
            var instalasiDk = _instalasiDkDal.GetData(request)
                ?? throw new KeyNotFoundException($"InstalasiDk id {request.InstalasiDkId} not found"); ;
            layanan.SetInstalasiDk(instalasiDk);

            _writer.Save(layanan);
            return Task.CompletedTask;
        }
    }
}
