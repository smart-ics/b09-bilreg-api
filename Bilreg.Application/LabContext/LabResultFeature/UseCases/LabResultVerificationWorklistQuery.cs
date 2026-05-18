using Bilreg.Application.LabContext.LabResultFeature;
using MediatR;

namespace Bilreg.Application.LabContext.LabResultFeature.UseCases;

public record LabResultVerificationWorklistQuery(
    string? SearchTerm,
    DateTime? Date1,
    DateTime? Date2) : IRequest<IEnumerable<LabResultVerificationWorklistView>>;

public class LabResultVerificationWorklistHandler
    : IRequestHandler<LabResultVerificationWorklistQuery, IEnumerable<LabResultVerificationWorklistView>>
{
    private readonly ILabResultVerificationWorklistDal _dal;

    public LabResultVerificationWorklistHandler(ILabResultVerificationWorklistDal dal)
    {
        _dal = dal;
    }

    public Task<IEnumerable<LabResultVerificationWorklistView>> Handle(
        LabResultVerificationWorklistQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new LabResultVerificationWorklistFilter(
            request.SearchTerm,
            request.Date1,
            request.Date2);

        return Task.FromResult(_dal.List(filter));
    }
}
