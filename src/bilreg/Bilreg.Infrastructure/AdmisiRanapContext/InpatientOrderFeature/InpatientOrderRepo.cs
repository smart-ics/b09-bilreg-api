using Bilreg.Application.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.InpatientOrderFeature;

public class InpatientOrderRepo : IInpatientOrderRepo
{
    private readonly IInpatientOrderDal _dal;

    public InpatientOrderRepo(IInpatientOrderDal dal)
    {
        _dal = dal;
    }

    public void SaveChanges(InpatientOrderModel model)
    {
        var existing = _dal.GetData(model);
        if (existing is null)
            _dal.Insert(InpatientOrderDto.FromModel(model));
        else
            _dal.Update(InpatientOrderDto.FromModel(model));
    }

    public MayBe<InpatientOrderModel> LoadEntity(IInpatientOrderKey key)
    {
        var dto = _dal.GetData(key);
        if (dto is null)
            return MayBe<InpatientOrderModel>.None;

        return MayBe.From(dto.ToModel());
    }

    public MayBe<InpatientOrderModel> GetByKey(IInpatientOrderKey key) => LoadEntity(key);

    public MayBe<InpatientOrderModel> GetByKey(string inpatientOrderId) =>
        LoadEntity(InpatientOrderModel.Key(inpatientOrderId));

    public IEnumerable<InpatientOrderModel> ListActive()
    {
        var listDto = _dal.ListActive()?.ToList() ?? [];
        return listDto.Select(x => x.ToModel()).ToList();
    }

    public IEnumerable<InpatientOrderModel> ListData(InpatientOrderListFilter filter)
    {
        var listDto = _dal.ListData(filter)?.ToList() ?? [];
        return listDto.Select(x => x.ToModel()).ToList();
    }

    public MayBe<InpatientOrderModel> GetByExternalReference(string externalReferenceId, string externalSystemSource)
    {
        var dto = _dal.GetByExternalReference(externalReferenceId, externalSystemSource);
        if (dto is null)
            return MayBe<InpatientOrderModel>.None;

        return MayBe.From(dto.ToModel());
    }

    public IEnumerable<InpatientOrderAuditRecord> ListAudits(string inpatientOrderId) =>
        _dal.ListAudits(inpatientOrderId).Select(a => new InpatientOrderAuditRecord(
            a.AuditId, a.EventTime, a.UserId, a.ActionType, a.Reason, a.OriginalDataJson));
}
