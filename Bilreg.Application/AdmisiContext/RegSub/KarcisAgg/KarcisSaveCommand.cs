using Bilreg.Application.AdmisiContext.LayananSub.InstalasiDkAgg;
using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiDkAgg;
using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;
using Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakAgg;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RegSub.KarcisAgg;

public record KarcisSaveCommand(string KarcisId, string KarcisName, string InstalasiDkId, string RekapCetakId):
    IRequest, IKarcisKey, IInstalasiDkKey, IRekapCetakKey;
    
public class KarcisSaveHandler: IRequestHandler<KarcisSaveCommand>
{
    private readonly IInstalasiDkDal _instalasiDkDal;
    // private readonly
    private readonly IFactoryLoadOrNull<KarcisModel, IKarcisKey> _factory;
    private readonly IKarcisWriter _writer;

    public KarcisSaveHandler(IFactoryLoadOrNull<KarcisModel, IKarcisKey> factory, IKarcisWriter writer)
    {
        _factory = factory;
        _writer = writer;
    }

    public Task Handle(KarcisSaveCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}