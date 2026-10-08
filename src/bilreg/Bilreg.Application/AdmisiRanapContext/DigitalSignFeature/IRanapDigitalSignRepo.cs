using Bilreg.Domain.AdmisiRanapContext.DigitalSignFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.AdmisiRanapContext.DigitalSignFeature;

public interface IRanapDigitalSignRepo :
    ISaveChange<RanapDigitalSignModel>,
    ILoadEntity<RanapDigitalSignModel, IRanapDigitalSignKey>
{
    MayBe<RanapDigitalSignModel> LoadByRegDokumen(string regId, string dokumenId);
    MayBe<RanapDigitalSignModel> LoadByExternalDoc(string regId, string dokumenId, string externalDocumentId);
    MayBe<RanapDigitalSignModel> LoadByOftaDocId(string oftaDocId);
    IEnumerable<RanapDigitalSignModel> ListByRegId(string regId);
}
