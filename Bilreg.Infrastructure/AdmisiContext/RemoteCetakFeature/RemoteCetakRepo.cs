using Bilreg.Application.AdmisiContext.RemoteCetakFeature;
using Bilreg.Domain.AdmisiContext.RemotCetakFeature;

namespace Bilreg.Infrastructure.AdmisiContext.RemoteCetakFeature;

public class RemoteCetakRepo : IRemoteCetakRepo
{
    private readonly IRemoteCetakDal _dal;

    public RemoteCetakRepo(IRemoteCetakDal dal)
    {
        _dal = dal;
    }

    public void SaveChanges(RemoteCetakType model)
    {
        _dal.Insert(RemoteCetakDto.FromModel(model));
    }
}