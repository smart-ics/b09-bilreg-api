using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;
using Bilreg.Domain.BillContext.TindakanSub.TarifAgg;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RegSub.KarcisAgg;

public record KarcisSetDefaultTarifCommand(string KarcisId, string TarifId): IRequest, IKarcisKey, ITarifKey;

public class KarcisSetDefaultTarifHandler: IRequestHandler<KarcisSetDefaultTarifCommand>
{
    public Task Handle(KarcisSetDefaultTarifCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}