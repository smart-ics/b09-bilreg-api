using Bilreg.Application.AdmisiContext.PetugasMedisSub.SmfAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SmfAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public record PetugasMedisSaveCommand(string PetugasMedisId, 
    string PetugasMedisName, string NamaSingkat, string SmfId) 
    : IRequest, IPetugasMedisKey, ISmfKey;

public class PetugasMedisSaveHandler : IRequestHandler<PetugasMedisSaveCommand>
{
    private readonly IFactoryLoadOrNull<PetugasMedisModel, IPetugasMedisKey> _factory;
    private readonly IPetugasMedisWriter _writer;
    private readonly ISmfDal _smfDal;

    public PetugasMedisSaveHandler(
        IFactoryLoadOrNull<PetugasMedisModel, IPetugasMedisKey> factory, 
        IPetugasMedisWriter writer, ISmfDal smfDal)
    {
        _factory = factory;
        _writer = writer;
        _smfDal = smfDal;
    }

    public Task Handle(PetugasMedisSaveCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.PetugasMedisId);
        Guard.IsNotWhiteSpace(request.PetugasMedisName);
        Guard.IsNotWhiteSpace(request.NamaSingkat);
        Guard.IsNotWhiteSpace(request.SmfId);
        var smf = _smfDal.GetData(request)
            ?? throw new KeyNotFoundException($"SMF {request.SmfId} not found");
        
        //  BUILD
        var petugasMedis = _factory.LoadOrNull(request) 
            ?? new PetugasMedisModel(request.PetugasMedisId, request.PetugasMedisName);
        petugasMedis.SetNama(request.PetugasMedisName, request.NamaSingkat);
        petugasMedis.Set(smf);
        
        //  WRITE
        _ = _writer.Save(petugasMedis);
        return Task.CompletedTask;
    }
}