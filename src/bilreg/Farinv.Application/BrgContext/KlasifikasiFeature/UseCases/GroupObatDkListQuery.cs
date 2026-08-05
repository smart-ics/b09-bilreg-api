using MediatR;

namespace Farinv.Application.BrgContext.KlasifikasiFeature.UseCases;

public record GroupObatDkListQuery() : IRequest<IEnumerable<GroupObatDkListResponse>>;

public record GroupObatDkListResponse(string GroupObatDkId, string GroupObatDkName);

public class GroupObatDkListHandler : IRequestHandler<GroupObatDkListQuery, IEnumerable<GroupObatDkListResponse>>
{
    private readonly IGroupObatDkRepo _groupObatDkRepo;

    public GroupObatDkListHandler(IGroupObatDkRepo groupObatDkRepo)
    {
        _groupObatDkRepo = groupObatDkRepo;
    }

    public Task<IEnumerable<GroupObatDkListResponse>> Handle(GroupObatDkListQuery request, CancellationToken cancellationToken)
    {
        var listGroupObatDk = _groupObatDkRepo.ListData()?.ToList() ?? [];
        var response = listGroupObatDk
            .OrderBy(x => x.GroupObatDkName)
            .Select(x => new GroupObatDkListResponse(x.GroupObatDkId, x.GroupObatDkName));

        return Task.FromResult(response);
    }
}