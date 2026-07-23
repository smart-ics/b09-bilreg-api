using Bilreg.Domain.AdmisiContext.AntrianFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record AdmissionServicePointResponse(string ServicePointId,string DisplayName,string QueuePrefix,string Status);
public record AdmissionServicePointListQuery(bool ActiveOnly=true):IRequest<IReadOnlyList<AdmissionServicePointResponse>>;
public record AdmissionServicePointUpsertCmd(string ServicePointId,string DisplayName,string QueuePrefix,bool Active):IRequest<AdmissionServicePointResponse>;

public sealed class AdmissionServicePointListHandler:IRequestHandler<AdmissionServicePointListQuery,IReadOnlyList<AdmissionServicePointResponse>>
{
    private readonly IAdmissionServicePointRepo _repo; public AdmissionServicePointListHandler(IAdmissionServicePointRepo r)=>_repo=r;
    public Task<IReadOnlyList<AdmissionServicePointResponse>> Handle(AdmissionServicePointListQuery q,CancellationToken ct)=>
      Task.FromResult<IReadOnlyList<AdmissionServicePointResponse>>(_repo.ListAll().Where(x=>!q.ActiveOnly||x.IsActive)
        .Select(x=>new AdmissionServicePointResponse(x.ServicePointId,x.DisplayName,x.QueuePrefix,x.Status.ToString())).ToList());
}
public sealed class AdmissionServicePointUpsertHandler:IRequestHandler<AdmissionServicePointUpsertCmd,AdmissionServicePointResponse>
{
    private readonly IAdmissionServicePointRepo _repo; public AdmissionServicePointUpsertHandler(IAdmissionServicePointRepo r)=>_repo=r;
    public Task<AdmissionServicePointResponse> Handle(AdmissionServicePointUpsertCmd q,CancellationToken ct)
    { var existing=_repo.LoadEntity(AdmissionServicePointModel.Key(q.ServicePointId));
      var model=existing.Match(x=>x.Rename(q.DisplayName).ChangePrefix(q.QueuePrefix),()=>AdmissionServicePointModel.Create(q.ServicePointId,q.DisplayName,q.QueuePrefix));
      if(!q.Active) model=model.Retire(); _repo.SaveChanges(model);
      return Task.FromResult(new AdmissionServicePointResponse(model.ServicePointId,model.DisplayName,model.QueuePrefix,model.Status.ToString())); }
}
