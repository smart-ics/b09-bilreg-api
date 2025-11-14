using Bilreg.Domain.AdmisiContext.LayananFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.LayananFeature;

public record GroupSpesialisListQuery() : IRequest<IEnumerable<GroupSpesialisType>>;

public class GroupSpesialisListHandler : IRequestHandler<GroupSpesialisListQuery, IEnumerable<GroupSpesialisType>>
{
    private readonly IGroupSpesialisRepo _repo;

    public GroupSpesialisListHandler(IGroupSpesialisRepo repo)
    {
        _repo = repo;
    }

    public Task<IEnumerable<GroupSpesialisType>> Handle(GroupSpesialisListQuery request, CancellationToken cancellationToken)
     {
        var listData = _repo.ListData()?.ToList()
            ?? throw new KeyNotFoundException("data not found");
        return Task.FromResult(listData.AsEnumerable());
    }
}
