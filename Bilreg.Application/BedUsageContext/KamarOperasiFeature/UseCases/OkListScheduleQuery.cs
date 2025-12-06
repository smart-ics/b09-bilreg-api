using MediatR;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkListScheduleQuery(string TglYmd) : IRequest<IEnumerable<OkListScheduleResponse>>;

public record OkListScheduleResponse(
    string PasienId, string PasienName, string TglLahir, 
    string NamaOperasi, string Urgency, 
    int Durasi, string StartTime,
    string DokterId, string DokterName,
    string KamarId, string KamarName);

public class OkListScheduleHandler : IRequestHandler<OkListScheduleQuery, IEnumerable<OkListScheduleResponse>>
{
    public Task<IEnumerable<OkListScheduleResponse>> Handle(OkListScheduleQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(ScheduleFaker.Generate(request.TglYmd));
    }
    
}