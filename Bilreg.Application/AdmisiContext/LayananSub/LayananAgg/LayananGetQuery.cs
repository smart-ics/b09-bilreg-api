using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using MediatR;

namespace Bilreg.Application.AdmisiContext.LayananSub.LayananAgg
{
    public record LayananGetQuery( string LayananId) : IRequest<LayananGetResponse>,ILayananKey;

    public record LayananGetResponse(
        string LayananId,
        string LayananName,
        bool IsAktif,
        string InstalasiId,
        string InstalasiName,
        string LayananDkId,
        string LayananDkName,
        string LayananTipeDkId,
        string LayananTipeDkName,
        string SmfId,
        string SmfName,
        string PetugasMedisId,
        string PetugasMedisName
        );
    public class LayananGetHandler : IRequestHandler<LayananGetQuery, LayananGetResponse>
    {
        private readonly ILayananDal _layananDal;

        public LayananGetHandler(ILayananDal layananDal)
        {
            _layananDal = layananDal;
        }

        public Task<LayananGetResponse> Handle(LayananGetQuery request, CancellationToken cancellationToken)
        {
            var layanan = _layananDal.GetData(request)
                ?? throw new KeyNotFoundException($"Layanan with id {request.LayananId} not found.");

            var response = new LayananGetResponse(
                layanan.LayananId,
                layanan.LayananName,
                layanan.IsAktif,
                layanan.InstalasiId,
                layanan.InstalasiName,
                layanan.LayananDkId,
                layanan.LayananDkName,
                layanan.LayananTipeDkId,
                layanan.LayananTipeDkName,
                layanan.SmfId,
                layanan.SmfName,
                layanan.PetugasMedisId,
                layanan.PetugasMedisName
                );
            return Task.FromResult(response);
        }
    }
}
