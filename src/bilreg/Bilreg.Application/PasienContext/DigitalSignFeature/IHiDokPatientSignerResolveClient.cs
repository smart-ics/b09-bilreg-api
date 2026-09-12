using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.PasienContext.DigitalSignFeature;

public interface IHiDokPatientSignerResolveClient :
    INunaService<HiDokPatientSignerResolveResponse, HiDokPatientSignerResolveRequest>
{
}

public record HiDokPatientSignerResolveRequest(string HospitalId, string NoMr);

public record HiDokPatientSignerResolveResponse(
    HiDokPatientSignerResolveStatus Status,
    string UserrId,
    string SignerId,
    string ErrorMessage);

public enum HiDokPatientSignerResolveStatus
{
    Success,
    NotFound,
    NotVerified,
    ProvisionFailed,
    Unauthorized,
    Error
}