using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record AdmissionQueueOperationResponse(string AntrianId, int NoUrut, string Status);
public record AdmissionQueueCallCmd(string AntrianId,int NoUrut,string LoketKey,string UserId)
    : IRequest<AdmissionQueueOperationResponse>;
public record AdmissionQueueRecallCmd(string AntrianId,int NoUrut,string LoketKey,byte[] ExpectedRowVersion,string UserId)
    : IRequest<AdmissionQueueOperationResponse>;
public record AdmissionQueueStartServiceCmd(string AntrianId,int NoUrut,string LoketKey,byte[] ExpectedRowVersion,string UserId)
    : IRequest<AdmissionQueueOperationResponse>;
public record AdmissionQueueWithdrawCmd(string AntrianId,int NoUrut,string Reason,string? LoketKey,byte[]? ExpectedRowVersion,string UserId)
    : IRequest<AdmissionQueueOperationResponse>;
public record AdmissionQueueNoShowCmd(string AntrianId,int NoUrut,string LoketKey,byte[] ExpectedRowVersion,string UserId)
    : IRequest<AdmissionQueueOperationResponse>;
public record AdmissionQueueRedirectCmd(string AntrianId,int NoUrut,string TargetServicePointId,
    string? LoketKey,byte[]? ExpectedRowVersion,string UserId)
    : IRequest<AdmissionQueueOperationResponse>;

public abstract class AdmissionQueueOperationHandlerBase
{
    protected readonly IAntrianRepo Queues; protected readonly IAdmissionQueueOperationRepo Operations;
    protected readonly ITglJamProvider Clock; protected readonly IAdmissionQueueRefreshPublisher Publisher;
    protected AdmissionQueueOperationHandlerBase(IAntrianRepo q,IAdmissionQueueOperationRepo o,
        ITglJamProvider c,IAdmissionQueueRefreshPublisher p) => (Queues,Operations,Clock,Publisher)=(q,o,c,p);
    protected AntrianEntryModel Entry(string q,int n) => Queues.LoadEntity(AntrianModel.Key(q))
        .GetValueOrThrow($"Admission queue '{q}' not found").ListEntry.FirstOrDefault(x=>x.NoUrut==n)
        ?? throw new KeyNotFoundException($"Queue entry '{q}' / {n} not found");
    protected static void Valid(string q,int n,string user) { Guard.Against.NullOrWhiteSpace(q); Guard.Against.NullOrWhiteSpace(user); if(n<=0) throw new ArgumentOutOfRangeException(nameof(n)); }
    protected static void Conflict(string q,int n) => throw new AdmissionQueueConcurrencyException($"Queue entry '{q}' / {n} changed concurrently or its Loket claim is stale.");
}

public sealed class AdmissionQueueCallHandler : AdmissionQueueOperationHandlerBase,
    IRequestHandler<AdmissionQueueCallCmd,AdmissionQueueOperationResponse>
{
    public AdmissionQueueCallHandler(IAntrianRepo q,IAdmissionQueueOperationRepo o,ITglJamProvider c,IAdmissionQueueRefreshPublisher p):base(q,o,c,p){}
    public async Task<AdmissionQueueOperationResponse> Handle(AdmissionQueueCallCmd r,CancellationToken ct)
    { Valid(r.AntrianId,r.NoUrut,r.UserId); Guard.Against.NullOrWhiteSpace(r.LoketKey); Entry(r.AntrianId,r.NoUrut).RecordCall();
      using(var t=TransHelper.NewScope()){if(!Operations.TryCall(r.AntrianId,r.NoUrut,r.LoketKey,r.UserId,Clock.Now)) Conflict(r.AntrianId,r.NoUrut);t.Complete();}
      await Publisher.PublishAsync(r.LoketKey,ct); return new(r.AntrianId,r.NoUrut,"Outstanding"); }
}

public sealed class AdmissionQueueRecallHandler : AdmissionQueueOperationHandlerBase,
    IRequestHandler<AdmissionQueueRecallCmd,AdmissionQueueOperationResponse>
{
    public AdmissionQueueRecallHandler(IAntrianRepo q,IAdmissionQueueOperationRepo o,ITglJamProvider c,IAdmissionQueueRefreshPublisher p):base(q,o,c,p){}
    public async Task<AdmissionQueueOperationResponse> Handle(AdmissionQueueRecallCmd r,CancellationToken ct)
    { Valid(r.AntrianId,r.NoUrut,r.UserId); Entry(r.AntrianId,r.NoUrut).RecordCall();
      using(var t=TransHelper.NewScope()){if(!Operations.TryRecall(r.AntrianId,r.NoUrut,r.LoketKey,r.ExpectedRowVersion,r.UserId,Clock.Now)) Conflict(r.AntrianId,r.NoUrut);t.Complete();}
      await Publisher.PublishAsync(r.LoketKey,ct); return new(r.AntrianId,r.NoUrut,"Outstanding"); }
}

public sealed class AdmissionQueueStartServiceHandler : AdmissionQueueOperationHandlerBase,
    IRequestHandler<AdmissionQueueStartServiceCmd,AdmissionQueueOperationResponse>
{
    public AdmissionQueueStartServiceHandler(IAntrianRepo q,IAdmissionQueueOperationRepo o,ITglJamProvider c,IAdmissionQueueRefreshPublisher p):base(q,o,c,p){}
    public async Task<AdmissionQueueOperationResponse> Handle(AdmissionQueueStartServiceCmd r,CancellationToken ct)
    { Valid(r.AntrianId,r.NoUrut,r.UserId); var at=Clock.Now; Entry(r.AntrianId,r.NoUrut).StartCalledService(at);
      using(var t=TransHelper.NewScope()){if(!Operations.TryStartService(r.AntrianId,r.NoUrut,r.LoketKey,r.ExpectedRowVersion,r.UserId,at)) Conflict(r.AntrianId,r.NoUrut);t.Complete();}
      await Publisher.PublishAsync(r.LoketKey,ct); return new(r.AntrianId,r.NoUrut,"InService"); }
}

public sealed class AdmissionQueueWithdrawHandler : AdmissionQueueOperationHandlerBase,
    IRequestHandler<AdmissionQueueWithdrawCmd,AdmissionQueueOperationResponse>
{
    public AdmissionQueueWithdrawHandler(IAntrianRepo q,IAdmissionQueueOperationRepo o,ITglJamProvider c,IAdmissionQueueRefreshPublisher p):base(q,o,c,p){}
    public async Task<AdmissionQueueOperationResponse> Handle(AdmissionQueueWithdrawCmd r,CancellationToken ct)
    { Valid(r.AntrianId,r.NoUrut,r.UserId); var at=Clock.Now; Entry(r.AntrianId,r.NoUrut).Withdraw(r.Reason,r.UserId,at);
      using(var t=TransHelper.NewScope()){if(!Operations.TryWithdraw(r.AntrianId,r.NoUrut,r.Reason,r.UserId,at,r.LoketKey,r.ExpectedRowVersion)) Conflict(r.AntrianId,r.NoUrut);t.Complete();}
      await Publisher.PublishAsync(r.LoketKey,ct); return new(r.AntrianId,r.NoUrut,"Withdrawn"); }
}

public sealed class AdmissionQueueNoShowHandler : AdmissionQueueOperationHandlerBase,
    IRequestHandler<AdmissionQueueNoShowCmd,AdmissionQueueOperationResponse>
{
    public AdmissionQueueNoShowHandler(IAntrianRepo q,IAdmissionQueueOperationRepo o,ITglJamProvider c,IAdmissionQueueRefreshPublisher p):base(q,o,c,p){}
    public async Task<AdmissionQueueOperationResponse> Handle(AdmissionQueueNoShowCmd r,CancellationToken ct)
    { Valid(r.AntrianId,r.NoUrut,r.UserId); var at=Clock.Now; Entry(r.AntrianId,r.NoUrut).Withdraw("NoShow",r.UserId,at);
      using(var t=TransHelper.NewScope()){if(!Operations.TryWithdraw(r.AntrianId,r.NoUrut,"NoShow",r.UserId,at,r.LoketKey,r.ExpectedRowVersion)) Conflict(r.AntrianId,r.NoUrut);t.Complete();}
      await Publisher.PublishAsync(r.LoketKey,ct); return new(r.AntrianId,r.NoUrut,"Withdrawn"); }
}

public sealed class AdmissionQueueRedirectHandler : AdmissionQueueOperationHandlerBase,
    IRequestHandler<AdmissionQueueRedirectCmd,AdmissionQueueOperationResponse>
{
    private readonly IAdmissionServicePointRepo _points; private readonly IAntrianFactory _factory;
    public AdmissionQueueRedirectHandler(IAntrianRepo q,IAdmissionQueueOperationRepo o,ITglJamProvider c,
      IAdmissionQueueRefreshPublisher p,IAdmissionServicePointRepo points,IAntrianFactory factory):base(q,o,c,p)=>(_points,_factory)=(points,factory);
    public async Task<AdmissionQueueOperationResponse> Handle(AdmissionQueueRedirectCmd r,CancellationToken ct)
    { Valid(r.AntrianId,r.NoUrut,r.UserId); var origin=Entry(r.AntrianId,r.NoUrut); var at=Clock.Now;
      origin.Withdraw("Redirected",r.UserId,at); var point=_points.LoadEntity(AdmissionServicePointModel.Key(r.TargetServicePointId)).GetValueOrThrow($"Admission Service Point '{r.TargetServicePointId}' not found"); point.EnsureCanAcceptIntake();
      var date=DateOnly.FromDateTime(at); var reference=new ServicePointType(point.ServicePointId,point.DisplayName); var tag=AntrianModel.GenSequenceTag(date,TimeOnly.MinValue,reference);
      var view=Queues.ListData(date).FirstOrDefault(x=>x.SequenceTag==tag); var target=view is null?_factory.Create(point,date):Queues.LoadEntity(view).Value;
      var replacement=target.AddAdmissionEntry(at); replacement.MarkRedirectReplacement(r.AntrianId,r.NoUrut);
      using(var t=TransHelper.NewScope()){if(!Operations.TryRedirect(r.AntrianId,r.NoUrut,r.UserId,at,r.LoketKey,r.ExpectedRowVersion,target,replacement)) Conflict(r.AntrianId,r.NoUrut);t.Complete();}
      await Publisher.PublishAsync(r.LoketKey,ct); return new(target.AntrianId,replacement.NoUrut,"Waiting"); }
}
