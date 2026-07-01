using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

public sealed class MergeRequestRepo : IMergeRequestRepo
{
    private const string SystemActor = "SYSTEM";

    private readonly IBilrgMergeRequestDal _dal;
    private readonly IRegRepo _regRepo;

    public MergeRequestRepo(IBilrgMergeRequestDal dal, IRegRepo regRepo)
    {
        _dal = dal;
        _regRepo = regRepo;
    }

    public void SaveChanges(MergeRequestModel model)
    {
        var patientId = ResolvePatientId(model.SourceRegId);
        var key = new MergeRequestKey(model.MergeRequestId);
        var existing = _dal.GetData(key);

        if (existing is null)
        {
            _dal.Insert(BilrgMergeRequestDto.FromModelForInsert(model, patientId, SystemActor));
            return;
        }

        var previousStatus = (MergeRequestStatusEnum)existing.Status;
        var dto = BilrgMergeRequestDto.FromModelForUpdate(model, patientId, SystemActor, previousStatus);
        _dal.Update(dto);
    }

    public MayBe<MergeRequestModel> LoadEntity(IMergeRequestKey key)
    {
        var row = _dal.GetData(key);
        if (row is null)
            return MayBe<MergeRequestModel>.None;

        return MayBe.From(row.ToModel());
    }

    public IEnumerable<MergeRequestModel> ListPendingByReg(IRegKey regKey) =>
        _dal.ListPendingByReg(regKey).Select(x => x.ToModel());

    public IEnumerable<MergeRequestModel> ListPendingByPatient(string pasienId) =>
        _dal.ListPendingByPatient(pasienId).Select(x => x.ToModel());

    private string ResolvePatientId(string sourceRegId)
    {
        var regKey = RegModel.Key(sourceRegId);
        return _regRepo.LoadEntity(regKey)
            .Match(onSome: r => r.Pasien.PasienId, onNone: () => string.Empty);
    }
}
