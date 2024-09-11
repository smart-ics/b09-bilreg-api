using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.AdmisiContext.LayananSub.LayananAgg
{
    public record LayananSetPetugasMedisCommand(string LayananId, string PetugasMedisId) : IRequest, ILayananKey, IPetugasMedisKey;

    public class LayananSetPetugasMedisHandler : IRequestHandler<LayananSetPetugasMedisCommand>
    {
        private readonly ILayananDal _layananDal;
        private readonly IPetugasMedisDal _petugasMedisDal;
        private readonly ILayananWriter _writer;

        public LayananSetPetugasMedisHandler(ILayananDal layananDal, IPetugasMedisDal petugasMedisDal, ILayananWriter writer)
        {
            _layananDal = layananDal;
            _petugasMedisDal = petugasMedisDal;
            _writer = writer;
        }


        public Task Handle(LayananSetPetugasMedisCommand request, CancellationToken cancellationToken)
        {
            Guard.IsNotNull(request);
            Guard.IsNotWhiteSpace(request.LayananId);
            Guard.IsNotWhiteSpace(request.PetugasMedisId);

            var layanan = _layananDal.GetData(request)
                ?? throw new KeyNotFoundException($"Layanan id {request.LayananId} not found");
            var petugas = _petugasMedisDal.GetData(request)
                ?? throw new KeyNotFoundException($"Petugas Medis id {request.PetugasMedisId} not found"); ;

            layanan.SetPetugasMedis(petugas);

            _writer.Save(layanan);
            return Task.CompletedTask;
        }
    }
}
