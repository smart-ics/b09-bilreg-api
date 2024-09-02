using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public record PetugasMedisSaveCommand(string PetugasMedisId, string PetugasMedisName) : IRequest, IPetugasMedisKey;

public class PetugasMedisSaveHandler : IRequestHandler<PetugasMedisSaveCommand>
{
    private readonly IFactoryLoadOrNull<PetugasMedisModel, IPetugasMedisKey> _factory;
    private readonly IPetugasMedisWriter _writer;

    public PetugasMedisSaveHandler(
        IFactoryLoadOrNull<PetugasMedisModel, IPetugasMedisKey> factory, 
        IPetugasMedisWriter writer)
    {
        _factory = factory;
        _writer = writer;
    }

    public Task Handle(PetugasMedisSaveCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.PetugasMedisId);
        Guard.IsNotWhiteSpace(request.PetugasMedisName);
        
        //  BUILD
        var petugasMedis = _factory.LoadOrNull(request) 
            ?? new PetugasMedisModel(request.PetugasMedisId, request.PetugasMedisName);
        
        //  WRITE
        _ = _writer.Save(petugasMedis);
        return Task.CompletedTask;
    }
}