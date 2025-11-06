using Bilreg.Application.Helpers;
using Bilreg.Application.ParamContext.ParamSistemAgg;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

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
    KtpType Ktp,
    KelurahanType Kelurahan,
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
    //private const string KODE_RS_PARAM_KEY = "RS__XXXXXX_KODE";

    public PasienGetHandler(IParamSistemDal paramSistemDal, 
        IPasienRepo pasienRepo, 
        IGetKodeRsService getKdRsSvc)
    {
        _paramSistemDal = paramSistemDal;
        _pasienRepo = pasienRepo;
        _getKdRsSvc = getKdRsSvc;
    }

    public Task<PasienGetResponse> Handle(PasienGetQuery request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsTrue(request.PasienId.IsValidA(x => x.Length is 6 or 8 or 15));

        var pasienId = GetPasienId(request.PasienId);

        var pasien = _pasienRepo.LoadEntity(PasienModel.Key(pasienId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Pasien id {request.PasienId} not found")
            );

        // RESPONSE
        var response = BuildPasienResponse(pasien);
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

    private static PasienGetResponse BuildPasienResponse(PasienModel pasien)
    {

        return new PasienGetResponse(
            pasien.PasienId,
            pasien.GetNomorMedrec(),
            pasien.Person.PersonName,
            pasien.TempatLahir,
            pasien.Person.TglLahir.ToString("yyyy-MM-dd"),
            pasien.GetUmur(),
            pasien.NickName,
            pasien.Person.Gender,
            pasien.NamaIbuKandung,
            pasien.GolDarah.ToString(),
            pasien.Ktp,
            pasien.Kelurahan,
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