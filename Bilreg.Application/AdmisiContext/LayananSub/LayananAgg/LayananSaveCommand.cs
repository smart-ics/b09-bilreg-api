using Bilreg.Application.AdmisiContext.LayananSub.InstalasiAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.AdmisiContext.LayananSub.LayananAgg
{
    public record LayananSaveCommand(
        string LayananId,
        string LayananName,
        string InstalasiId
        ) : IRequest, ILayananKey, IInstalasiKey;

    public class LayananSaveCommandHandler : IRequestHandler<LayananSaveCommand>
    {
        private readonly ILayananDal _layananDal;
        private readonly IInstalasiDal _instalasiDal;
        private readonly ILayananWriter _writer;

        public LayananSaveCommandHandler(ILayananDal layananDal,IInstalasiDal instalasiDal, ILayananWriter writer)
        {
            _layananDal = layananDal;
            _instalasiDal = instalasiDal;
            _writer = writer;
        }

        public Task Handle(LayananSaveCommand request, CancellationToken cancellationToken)
        {
            Guard.IsNotNull(request);
            Guard.IsNotWhiteSpace(request.LayananId);
            Guard.IsNotWhiteSpace(request.LayananName);
            Guard.IsNotWhiteSpace(request.InstalasiId);

            var instalasi = _instalasiDal.GetData(request)
                ?? throw new KeyNotFoundException($"Instalasi with id {request.InstalasiId} not found");

            var layanan = _layananDal.GetData(request)
                ?? new LayananModel(request.LayananId, request.LayananName);


            layanan.SetInstalasi(request.InstalasiId);
            _writer.Save(layanan);
            return Task.CompletedTask;
        }
    }
}
