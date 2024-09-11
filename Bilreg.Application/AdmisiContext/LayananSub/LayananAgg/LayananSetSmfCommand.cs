using Bilreg.Application.AdmisiContext.PetugasMedisSub.SmfAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SmfAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.AdmisiContext.LayananSub.LayananAgg
{
    public record LayananSetSmfCommand(string LayananId, string SmfId) : IRequest, ILayananKey, ISmfKey;

    public class LayananSetSmfHandler : IRequestHandler<LayananSetSmfCommand>
    {
        private readonly ILayananDal _layananDal;
        private readonly ISmfDal _smfDal;
        private readonly ILayananWriter _writer;

        public LayananSetSmfHandler(ILayananDal layananDal, ISmfDal smfDal, ILayananWriter writer)
        {
            _layananDal = layananDal;
            _smfDal = smfDal;
            _writer = writer;
        }


        public Task Handle(LayananSetSmfCommand request, CancellationToken cancellationToken)
        {
            Guard.IsNotNull(request);
            Guard.IsNotWhiteSpace(request.LayananId);
            Guard.IsNotWhiteSpace(request.SmfId);

            var layanan = _layananDal.GetData(request)
                ?? throw new KeyNotFoundException($"Layanan id {request.LayananId} not found");
            var smf = _smfDal.GetData(request)
                ?? throw new KeyNotFoundException($"Smf id {request.SmfId} not found"); ;

            layanan.SetSmf(smf);

            _writer.Save(layanan);
            return Task.CompletedTask;
        }
    }
}
