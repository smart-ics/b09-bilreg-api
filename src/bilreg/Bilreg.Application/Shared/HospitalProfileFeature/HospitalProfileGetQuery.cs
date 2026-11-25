using Bilreg.Domain.Shared.Param;
using MediatR;

namespace Bilreg.Application.Shared.HospitalProfileFeature;

public record HospitalProfileGetQuery() : IRequest<HospitalProfileResponse>;

public record HospitalProfileResponse(
    string KodeRs,
    string NamaRs,
    string Alamat,
    string Kota,
    string Telepon,
    string Email);

public class HospitalProfileGetHandler : IRequestHandler<HospitalProfileGetQuery, HospitalProfileResponse>
{
    private readonly IGetHospitalProfileService _service;

    public HospitalProfileGetHandler(IGetHospitalProfileService service)
    {
        _service = service;
    }

    public Task<HospitalProfileResponse> Handle(HospitalProfileGetQuery request, CancellationToken cancellationToken)
    {
        var model = _service.Execute();
        var response = new HospitalProfileResponse(
            model?.KodeRs ?? string.Empty,
            model?.NamaRs ?? string.Empty,
            model?.Alamat ?? string.Empty,
            model?.Kota ?? string.Empty,
            model?.Telepon ?? string.Empty,
            model?.Email ?? string.Empty);

        return Task.FromResult(response);
    }
}
