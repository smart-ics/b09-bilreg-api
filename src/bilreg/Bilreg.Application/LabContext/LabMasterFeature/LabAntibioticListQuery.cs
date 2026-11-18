using Bilreg.Domain.LabContext.LabMasterFeature;
using MediatR;

namespace Bilreg.Application.LabContext.LabMasterFeature;

public record LabAntibioticListResponse(
    string AntibioticId,
    string AntibioticName,
    string DiskConcentration,
    string ResistenRange,
    string IntermediateRange,
    string SensitifRange,
    int DisplayOrder);

public record LabAntibioticListQuery(string? Search = null)
    : IRequest<IEnumerable<LabAntibioticListResponse>>;

public class LabAntibioticListHandler
    : IRequestHandler<LabAntibioticListQuery, IEnumerable<LabAntibioticListResponse>>
{
    private readonly ILabAntibioticRepo _repo;

    public LabAntibioticListHandler(ILabAntibioticRepo repo)
    {
        _repo = repo;
    }

    public Task<IEnumerable<LabAntibioticListResponse>> Handle(
        LabAntibioticListQuery request,
        CancellationToken cancellationToken)
    {
        var antibiotics = string.IsNullOrWhiteSpace(request.Search)
            ? _repo.ListData()
            : _repo.ListData(request.Search);

        var response = antibiotics
            .Select(x => new LabAntibioticListResponse(
                x.AntibioticId,
                x.AntibioticName,
                x.DiskConcentration,
                x.ResistenRange,
                x.IntermediateRange,
                x.SensitifRange,
                x.DisplayOrder));

        return Task.FromResult(response);
    }
}
