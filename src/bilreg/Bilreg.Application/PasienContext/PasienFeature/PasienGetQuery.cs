using Bilreg.Application.Shared.Param.ParamSistemAgg;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Bilreg.Domain.Shared.Param;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienGetQuery(string PasienId) : IRequest<PasienGetResponse>, IPasienKey;

public record PasienGetResponse(
    string PasienId,
    string NomorMedrec,
    string PasienName,
    string TempatLahir,
    string TglLahir,
    string Umur,
    string NickName,
    string Gender,
    string IbuKandung,
    string GolDarah,
    string Email,
    string NoHp,
    bool IsAktif,
    AlamatType AlamatDomisili,
    KelurahanType Kelurahan,
    KtpType Ktp,
    IdentitasType KartuKeluarga,
    PasienKeluargaType Keluarga,
    StatusKawinDkType StatusKawin,
    AgamaType Agama,
    SukuType Suku,
    PekerjaanDkType Pekerjaan,
    PendidikanDkType Pendidikan
);

public class PasienGetHandler : IRequestHandler<PasienGetQuery, PasienGetResponse>
{
    private readonly IParamSistemDal _paramSistemDal;
    private readonly IPasienRepo _pasienRepo;
    private readonly IGetKodeRsService _getKdRsSvc;
    private readonly ITglJamProvider _tglJamProvider;
    //private const string KODE_RS_PARAM_KEY = "RS__XXXXXX_KODE";

    public PasienGetHandler(IParamSistemDal paramSistemDal, 
        IPasienRepo pasienRepo, 
        IGetKodeRsService getKdRsSvc,
        ITglJamProvider? tglJamProvider = null)
    {
        _paramSistemDal = paramSistemDal;
        _pasienRepo = pasienRepo;
        _getKdRsSvc = getKdRsSvc;
        _tglJamProvider = tglJamProvider;
    }

    public Task<PasienGetResponse> Handle(PasienGetQuery request, CancellationToken cancellationToken)
    {
        // BUILD
        var pasienId = GetPasienId(request.PasienId);

        var pasien = _pasienRepo.LoadEntity(PasienModel.Key(pasienId)).GetValueOrThrow($"Pasien id {request.PasienId} not found");

        // RESPONSE
        var businessDate = DateOnly.FromDateTime(_tglJamProvider.Now);
        var response = BuildPasienResponse(pasien, businessDate);
        return Task.FromResult(response);
    }

    private string GetPasienId(string pasienId)
    {
        var kodeRs = _getKdRsSvc.Execute();

        return pasienId.Length switch
        {
            6 => $"{kodeRs}00{pasienId}",
            8 => $"{kodeRs}{pasienId}",
            _ => pasienId
        };
    }

    private static PasienGetResponse BuildPasienResponse(PasienModel pasien, DateOnly businessDate)
    {
        
        return new PasienGetResponse(
            pasien.PasienId,
            pasien.GetNomorMedrec(),
            pasien.Person.PersonName,
            pasien.TempatLahir,
            pasien.Person.TglLahir.ToString("yyyy-MM-dd"),
            pasien.GetUmur(businessDate),
            pasien.NickName,
            pasien.Person.Gender,
            pasien.NamaIbuKandung,
            pasien.GolDarah.ToString(),
            pasien.ListContact.Where(x => x.JenisContact == JenisContactEnum.Email).First().ContactDetail,
            pasien.Person.Contact.ContactDetail,
            pasien.IsAktif,
            pasien.Person.Alamat,
            pasien.Kelurahan,
            pasien.Ktp,
            pasien.KartuKeluarga,
            pasien.PasienKeluarga,
            pasien.StatusKawin,
            pasien.Agama,
            pasien.Suku,
            pasien.PekerjaanDk,
            pasien.PendidikanDk
        );
    }
}
