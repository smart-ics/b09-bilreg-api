using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public record RegistrationOutcomeResponse(string OutcomeId,string AntrianId,int NoUrut,string OutcomeType,string Status);
public record FinalizeRegistrationEstablishedCmd(string AntrianId,int NoUrut,string LoketKey,
    byte[] ExpectedClaimVersion,string RegId,string UserId):IRequest<RegistrationOutcomeResponse>;

public abstract class RegistrationOutcomeHandlerBase
{
    protected readonly IAntrianRepo Queues; protected readonly IRegistrationOutcomeOperationRepo Outcomes;
    protected readonly ITglJamProvider Clock; protected readonly IAdmissionQueueRefreshPublisher Publisher;
    protected RegistrationOutcomeHandlerBase(IAntrianRepo q,IRegistrationOutcomeOperationRepo o,
        ITglJamProvider c,IAdmissionQueueRefreshPublisher p)=>(Queues,Outcomes,Clock,Publisher)=(q,o,c,p);
    protected void EnsureInService(string q,int n)
    {
        var entry=Queues.LoadEntity(AntrianModel.Key(q)).GetValueOrThrow($"Admission queue '{q}' not found")
            .ListEntry.FirstOrDefault(x=>x.NoUrut==n)??throw new KeyNotFoundException($"Queue entry '{q}' / {n} not found");
        if(entry.AntrianStatus!=AntrianStatusEnum.InService)
            throw new InvalidOperationException("A final Registration Outcome requires an InService Queue Entry.");
    }
    protected async Task<RegistrationOutcomeResponse> Finalize(RegistrationOutcomeModel outcome,
        string loket,byte[] version,CancellationToken ct)
    {
        using(var t=TransHelper.NewScope())
        { if(!Outcomes.TryFinalize(outcome,loket,version,outcome.CreatedAt))
            throw new AdmissionQueueConcurrencyException($"Queue entry '{outcome.AntrianId}' / {outcome.NoUrut} already has a final outcome or changed concurrently.");
          t.Complete(); }
        await Publisher.PublishAsync(loket,ct);
        return new(outcome.OutcomeId,outcome.AntrianId,outcome.NoUrut,outcome.OutcomeType.ToString(),"Done");
    }
}

public sealed class FinalizeRegistrationEstablishedHandler:RegistrationOutcomeHandlerBase,
    IRequestHandler<FinalizeRegistrationEstablishedCmd,RegistrationOutcomeResponse>
{
    private readonly IRegRepo _regs;
    public FinalizeRegistrationEstablishedHandler(IAntrianRepo q,IRegistrationOutcomeOperationRepo o,
      ITglJamProvider c,IAdmissionQueueRefreshPublisher p,IRegRepo regs):base(q,o,c,p)=>_regs=regs;
    public Task<RegistrationOutcomeResponse> Handle(FinalizeRegistrationEstablishedCmd r,CancellationToken ct)
    { Guard.Against.NullOrWhiteSpace(r.RegId); Guard.Against.NullOrWhiteSpace(r.UserId);
      EnsureInService(r.AntrianId,r.NoUrut);
      _regs.LoadEntity(RegModel.Key(r.RegId)).GetValueOrThrow($"Registration '{r.RegId}' not found");
      return Finalize(RegistrationOutcomeModel.Established(r.AntrianId,r.NoUrut,r.RegId,r.UserId,Clock.Now),r.LoketKey,r.ExpectedClaimVersion,ct); }
}

