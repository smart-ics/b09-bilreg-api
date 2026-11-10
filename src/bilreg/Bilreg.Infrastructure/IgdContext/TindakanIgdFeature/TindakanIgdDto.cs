using Bilreg.Application.IgdContext.TindakanIgdFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.IgdContext.TindakanIgdFeature;

public record TindakanIgdDto(
    string TindakanIgdId,
    string IgdVisitId,
    string RegId,
    string ReffId,
    string Descriptions,
    int Qty,
    int Aktifitas,
    string PpaId, 
    string PpaName,
    string? TrsTdkId,
    string? TrsBillId,
    bool IsPosted,
    DateTime? PostedDate,
    string CrtUser,
    DateTime CrtDate
    )
{
    public TindakanIgdDto() : this("", "", "", "", "", 0, 0, "", "", null, null, false, null, "", default) { }

    public static TindakanIgdDto FromModel(TindakanIgdModel model)
        => new(
            TindakanIgdId: model.TindakanIgdId,
            IgdVisitId: model.IgdVisitId,
            RegId: model.RegId,
            ReffId: model.ReffId,
            Descriptions: model.Descriptions,
            Qty: model.Qty,
            Aktifitas: (int)model.Aktifitas,
            PpaId: model.Ppa.PpaId,
            PpaName: model.Ppa.PpaName,
            TrsTdkId: model.TrsTdkId,
            TrsBillId: model.TrsBillId,
            IsPosted: model.IsPosted,
            PostedDate: model.PostedDate,
            CrtUser: model.Audit.UserId,
            CrtDate: model.Audit.Timestamp);

    public TindakanIgdModel ToModel()
    {
        var audit = new AuditInfoType(CrtUser, CrtDate);
        var petugasMedis = new PpaReff(PpaId, PpaName);

        var result = new TindakanIgdModel(
            tindakanIgdId: TindakanIgdId,
            igdVisitId: IgdVisitId,
            regId: RegId,
            reffId: ReffId,
            descriptions: Descriptions,
            qty: Qty,
            aktifitas: (ActivityTindakanIgd)Aktifitas,
            ppa: petugasMedis,
            audit: audit,
            trsTdkId: TrsTdkId ?? string.Empty,
            trsBillId: TrsBillId ?? string.Empty,
            isPosted: IsPosted,
            postedDate: PostedDate);
        return result;
    }

    public TindakanIgdView ToView()
        => new(
            TindakanIgdId: TindakanIgdId,
            IgdVisitId: IgdVisitId,
            RegId: RegId,
            ReffId: ReffId,
            Descriptions: Descriptions,
            Qty: Qty,
            Aktifitas: (ActivityTindakanIgd)Aktifitas,
            Ppa: new PpaReff(PpaId, PpaName),
            CrtUserId: CrtUser,
            CreatedDateTime: CrtDate,
            TrsTdkId: TrsTdkId ?? string.Empty,
            TrsBillId: TrsBillId ?? string.Empty,
            IsPosted: IsPosted,
            PostedDate: PostedDate);
}
